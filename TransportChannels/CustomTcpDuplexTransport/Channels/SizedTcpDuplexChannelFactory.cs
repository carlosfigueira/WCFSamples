using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;

namespace CustomTcpDuplex.Channels
{
    class SizedTcpDuplexChannelFactory : ChannelFactoryBase<IDuplexChannel>
    {
        BufferManager bufferManager;
        MessageEncoderFactory encoderFactory;
        public SizedTcpDuplexChannelFactory(SizedTcpDuplexTransportBindingElement bindingElement, BindingContext context)
            : base(context.Binding)
        {
            // populate members from binding element
            int maxBufferSize = (int)bindingElement.MaxReceivedMessageSize;
            this.bufferManager = BufferManager.CreateBufferManager(bindingElement.MaxBufferPoolSize, maxBufferSize);

            Collection<MessageEncodingBindingElement> messageEncoderBindingElements
                = context.BindingParameters.FindAll<MessageEncodingBindingElement>();

            if (messageEncoderBindingElements.Count > 1)
            {
                throw new InvalidOperationException("More than one MessageEncodingBindingElement was found in the BindingParameters of the BindingContext");
            }
            else if (messageEncoderBindingElements.Count == 1)
            {
                if (messageEncoderBindingElements[0].MessageVersion != MessageVersion.Soap12WSAddressing10)
                {
                    throw new InvalidOperationException("This transport must be used with the an encoding with MessageVersion.Soap12WSAddressing10.");
                }

                this.encoderFactory = messageEncoderBindingElements[0].CreateMessageEncoderFactory();
            }
            else
            {
                this.encoderFactory = new TextMessageEncodingBindingElement(MessageVersion.Soap12WSAddressing10, Encoding.UTF8).CreateMessageEncoderFactory();
            }
        }

        protected override IDuplexChannel OnCreateChannel(EndpointAddress address, Uri via)
        {
            return new SizedTcpDuplexClientChannel(
                this.encoderFactory.Encoder,
                this.bufferManager,
                this,
                address,
                via);
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.OnOpen(timeout);
            return new CompletedAsyncResult(callback, state);
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            CompletedAsyncResult.End(result);
        }

        protected override void OnOpen(TimeSpan timeout)
        {
        }

        class SizedTcpDuplexClientChannel : SizedTcpDuplexChannel
        {
            public SizedTcpDuplexClientChannel(
                MessageEncoder encoder,
                BufferManager bufferManager,
                ChannelManagerBase channelManager,
                EndpointAddress remoteAddress,
                Uri via) :
                base(encoder, bufferManager, channelManager, null, remoteAddress, via)
            {
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

            class ConnectAsyncResult : AsyncResult
            {
                TimeSpan timeout;
                SizedTcpDuplexClientChannel channel;
                IPHostEntry hostEntry;
                Socket socket;
                bool connected;
                int currentEntry;
                int port;

                public ConnectAsyncResult(TimeSpan timeout, SizedTcpDuplexClientChannel channel, AsyncCallback callback, object state)
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
}
