using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace JsonRpcOverTcp
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
            return this.BeginRequest(message, this.DefaultReceiveTimeout, callback, state);
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
            base.Send(message, timeout);
            return base.Receive(timeout);
        }

        public Message Request(Message message)
        {
            return this.Request(message, this.DefaultReceiveTimeout);
        }

        public Uri Via
        {
            get { return this.via; }
        }

        void Connect()
        {
            Socket socket = null;
            int port = Via.Port;
            if (port == -1)
            {
                port = 8081; // the default port used by WSE 3.0
            }

            IPHostEntry hostEntry;

            try
            {
                hostEntry = Dns.GetHostEntry(Via.Host);
            }
            catch (SocketException socketException)
            {
                throw new EndpointNotFoundException("Unable to resolve host" + Via.Host, socketException);
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

        protected override void OnOpen(TimeSpan timeout)
        {
            Connect();
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

                IAsyncResult sendResult = channel.BeginSend(message, timeout, OnSend, this);
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
                this.channel.EndSend(asyncResult);

                IAsyncResult receiveResult = this.channel.BeginReceive(this.timeout, OnReceive, this);
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

                thisPtr.Dump();
                thisPtr.Complete(false, completeException);
            }

            void CompleteReceive(IAsyncResult asyncResult)
            {
                this.response = this.channel.EndReceive(asyncResult);
            }

            internal static Message End(IAsyncResult result)
            {
                RequestAsyncResult thisPtr = AsyncResult.End<RequestAsyncResult>(result);
                return thisPtr.response;
            }

            void Dump()
            {
                using (FileStream fs = File.Open(@"c:\temp\log.txt", FileMode.Append, FileAccess.Write, FileShare.None))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.WriteLine("Call stack for {0}", this.GetHashCode());
                        sw.WriteLine(Environment.StackTrace);
                    }
                }
            }
        }
    }
}
