using System;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace JsonRpcOverTcp.Channels
{
    class SizedTcpRequestChannel : SizedTcpBaseChannel, IRequestChannel
    {
        Uri via;
        EndpointAddress remoteAddress;

        public SizedTcpRequestChannel(MessageEncoder encoder, BufferManager bufferManager, ChannelManagerBase channelManager, EndpointAddress remoteAddress, Uri via)
            : base(encoder, bufferManager, channelManager)
        {
            this.via = via;
            this.remoteAddress = remoteAddress;
        }

        public IAsyncResult BeginRequest(Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new RequestAsyncResult(message, timeout, this, callback, state);
        }

        public IAsyncResult BeginRequest(Message message, AsyncCallback callback, object state)
        {
            return this.BeginRequest(message, base.DefaultSendTimeout, callback, state);
        }

        public Message EndRequest(IAsyncResult result)
        {
            return RequestAsyncResult.End(result);
        }

        public EndpointAddress RemoteAddress
        {
            get { return this.remoteAddress; }
        }

        public Message Request(Message message, TimeSpan timeout)
        {
            base.SendMessage(message, timeout);
            return base.ReceiveMessage(timeout);
        }

        public Message Request(Message message)
        {
            return this.Request(message, base.DefaultSendTimeout);
        }

        public Uri Via
        {
            get { return this.via; }
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            this.Connect();
            base.OnOpen(timeout);
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new ConnectAsyncResult(timeout, this, callback, state);
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            ConnectAsyncResult.End(result);
        }

        void Connect()
        {
            Socket socket = null;
            int port = Via.Port;
            if (port == -1)
            {
                port = 8000; // the default port for sized.tcp
            }

            IPHostEntry hostEntry;

            try
            {
                hostEntry = Dns.GetHostEntry(this.Via.Host);
            }
            catch (SocketException socketException)
            {
                throw new EndpointNotFoundException("Unable to resolve host: " + this.Via.Host, socketException);
            }

            for (int i = 0; i < hostEntry.AddressList.Length; i++)
            {
                try
                {
                    IPAddress address = hostEntry.AddressList[i];
                    socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect(new IPEndPoint(address, port));
                    break;
                }
                catch (SocketException socketException)
                {
                    if (i == hostEntry.AddressList.Length - 1)
                    {
                        throw ConvertSocketException(socketException, "Connect");
                    }
                }
            }

            base.InitializeSocket(socket);
        }

        class RequestAsyncResult : AsyncResult
        {
            private Message response;
            private TimeSpan timeout;
            private SizedTcpRequestChannel channel;

            public RequestAsyncResult(Message message, TimeSpan timeout, SizedTcpRequestChannel channel, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.channel = channel;
                this.timeout = timeout;

                IAsyncResult sendResult = channel.BeginSendMessage(message, timeout, OnSend, this);
                if (sendResult.CompletedSynchronously)
                {
                    this.CompleteSend(sendResult);
                }
            }

            static void OnSend(IAsyncResult asyncResult)
            {
                if (asyncResult.CompletedSynchronously)
                {
                    return;
                }

                RequestAsyncResult thisPtr = (RequestAsyncResult)asyncResult.AsyncState;
                Exception completeException = null;
                bool completeSelf = false;
                try
                {
                    completeSelf = thisPtr.CompleteSend(asyncResult);
                }
                catch (Exception e)
                {
                    completeException = e;
                    completeSelf = true;
                }

                if (completeSelf)
                {
                    thisPtr.Complete(false, completeException);
                }
            }

            bool CompleteSend(IAsyncResult asyncResult)
            {
                this.channel.EndSendMessage(asyncResult);

                IAsyncResult receiveResult = this.channel.BeginReceiveMessage(this.timeout, OnReceive, this);
                if (!receiveResult.CompletedSynchronously)
                {
                    return false;
                }

                this.CompleteReceive(asyncResult);
                return false;
            }

            static void OnReceive(IAsyncResult asyncResult)
            {
                if (asyncResult.CompletedSynchronously)
                {
                    return;
                }

                RequestAsyncResult thisPtr = (RequestAsyncResult)asyncResult.AsyncState;
                Exception completeException = null;
                try
                {
                    thisPtr.CompleteReceive(asyncResult);
                }
                catch (Exception e)
                {
                    completeException = e;
                }

                thisPtr.Complete(false, completeException);
            }

            void CompleteReceive(IAsyncResult asyncResult)
            {
                this.response = this.channel.EndReceiveMessage(asyncResult);
            }

            internal static Message End(IAsyncResult result)
            {
                RequestAsyncResult thisPtr = AsyncResult.End<RequestAsyncResult>(result);
                return thisPtr.response;
            }
        }

        class ConnectAsyncResult : AsyncResult
        {
            TimeSpan timeout;
            SizedTcpRequestChannel channel;
            IPHostEntry hostEntry;
            Socket socket;
            bool connected;
            int currentEntry;
            int port;

            public ConnectAsyncResult(TimeSpan timeout, SizedTcpRequestChannel channel, AsyncCallback callback, object state)
                : base(callback, state)
            {
                // production code should use this timeout
                this.timeout = timeout;
                this.channel = channel;

                IAsyncResult dnsGetHostResult = Dns.BeginGetHostEntry(channel.Via.Host, OnDnsGetHost, this);
                if (!dnsGetHostResult.CompletedSynchronously)
                {
                    return;
                }

                if (this.CompleteDnsGetHost(dnsGetHostResult))
                {
                    base.Complete(true);
                }
            }

            static void OnDnsGetHost(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                ConnectAsyncResult thisPtr = (ConnectAsyncResult)result.AsyncState;

                Exception completionException = null;
                bool completeSelf = false;
                try
                {
                    completeSelf = thisPtr.CompleteDnsGetHost(result);
                }
                catch (Exception e)
                {
                    completeSelf = true;
                    completionException = e;
                }

                if (completeSelf)
                {
                    thisPtr.Complete(false, completionException);
                }
            }

            bool CompleteDnsGetHost(IAsyncResult result)
            {
                try
                {
                    this.hostEntry = Dns.EndGetHostEntry(result);
                }
                catch (SocketException socketException)
                {
                    throw new EndpointNotFoundException("Unable to resolve host" + channel.Via.Host, socketException);
                }

                port = this.channel.Via.Port;
                if (port == -1)
                {
                    port = 8000; // Let's call it the default port for our protocol
                }

                IAsyncResult socketConnectResult = this.BeginSocketConnect();
                if (!socketConnectResult.CompletedSynchronously)
                {
                    return false;
                }

                return this.CompleteSocketConnect(socketConnectResult);
            }

            IAsyncResult BeginSocketConnect()
            {
                IPAddress address = this.hostEntry.AddressList[this.currentEntry];
                this.socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                while (true)
                {
                    try
                    {
                        return this.socket.BeginConnect(new IPEndPoint(address, this.port), OnSocketConnect, this);
                    }
                    catch (SocketException socketException)
                    {
                        if (this.currentEntry == this.hostEntry.AddressList.Length - 1)
                        {
                            throw ConvertSocketException(socketException, "Connect");
                        }

                        this.currentEntry++;
                    }
                }
            }

            static void OnSocketConnect(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                ConnectAsyncResult thisPtr = (ConnectAsyncResult)result.AsyncState;

                Exception completionException = null;
                bool completeSelf = false;
                try
                {
                    completeSelf = thisPtr.CompleteSocketConnect(result);
                }
                catch (Exception e)
                {
                    completeSelf = true;
                    completionException = e;
                }

                if (completeSelf)
                {
                    thisPtr.Complete(false, completionException);
                }
            }

            bool CompleteSocketConnect(IAsyncResult result)
            {
                while (!this.connected && this.currentEntry < this.hostEntry.AddressList.Length)
                {
                    try
                    {
                        socket.EndConnect(result);
                        connected = true;
                        break;
                    }
                    catch (SocketException socketException)
                    {
                        if (this.currentEntry == this.hostEntry.AddressList.Length - 1)
                        {
                            throw ConvertSocketException(socketException, "Connect");
                        }

                        this.currentEntry++;
                    }

                    result = BeginSocketConnect();
                    if (!result.CompletedSynchronously)
                    {
                        return false;
                    }
                }

                this.channel.InitializeSocket(socket);
                return true;
            }

            public static void End(IAsyncResult result)
            {
                AsyncResult.End<ConnectAsyncResult>(result);
            }
        }
    }
}
