using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;

namespace CustomTcpDuplex.Channels
{
    class SizedTcpDuplexChannelListener : ChannelListenerBase<IDuplexChannel>
    {
        BufferManager bufferManager;
        MessageEncoderFactory encoderFactory;
        Socket listenSocket;
        Uri uri;

        public SizedTcpDuplexChannelListener(SizedTcpDuplexTransportBindingElement bindingElement, BindingContext context)
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

            this.uri = new Uri(context.ListenUriBaseAddress, context.ListenUriRelativeAddress);
        }

        protected override IDuplexChannel OnAcceptChannel(TimeSpan timeout)
        {
            try
            {
                Socket dataSocket = listenSocket.Accept();
                return new SizedTcpDuplexServerChannel(this.encoderFactory.Encoder, this.bufferManager, new EndpointAddress(this.uri), dataSocket, this);
            }
            catch (ObjectDisposedException)
            {
                // socket closed
                return null;
            }
        }

        protected override IAsyncResult OnBeginAcceptChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new AcceptChannelAsyncResult(timeout, this, callback, state);
        }

        protected override IDuplexChannel OnEndAcceptChannel(IAsyncResult result)
        {
            return AcceptChannelAsyncResult.End(result);
        }

        protected override IAsyncResult OnBeginWaitForChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            throw new NotSupportedException("No peeking support");
        }

        protected override bool OnEndWaitForChannel(IAsyncResult result)
        {
            throw new NotSupportedException("No peeking support");
        }

        protected override bool OnWaitForChannel(TimeSpan timeout)
        {
            throw new NotSupportedException("No peeking support");
        }

        public override Uri Uri
        {
            get { return this.uri; }
        }

        protected override void OnAbort()
        {
            this.CloseListenSocket(TimeSpan.Zero);
        }

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.CloseListenSocket(timeout);
            return new CompletedAsyncResult(callback, state);
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.OpenListenSocket();
            return new CompletedAsyncResult(callback, state);
        }

        protected override void OnClose(TimeSpan timeout)
        {
            this.CloseListenSocket(timeout);
        }

        protected override void OnEndClose(IAsyncResult result)
        {
            CompletedAsyncResult.End(result);
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            CompletedAsyncResult.End(result);
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            this.OpenListenSocket();
        }

        void OpenListenSocket()
        {
            IPEndPoint localEndpoint = new IPEndPoint(IPAddress.Any, uri.Port);
            this.listenSocket = new Socket(localEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            this.listenSocket.Bind(localEndpoint);
            this.listenSocket.Listen(10);
        }

        void CloseListenSocket(TimeSpan timeout)
        {
            this.listenSocket.Close((int)timeout.TotalMilliseconds);
        }

        class SizedTcpDuplexServerChannel : SizedTcpDuplexChannel
        {
            Socket dataSocket;
            public SizedTcpDuplexServerChannel(MessageEncoder messageEncoder, BufferManager bufferManager, EndpointAddress localAddress, Socket dataSocket, ChannelManagerBase channelManager)
                :base(messageEncoder, bufferManager, channelManager, localAddress, null, null)
            {
                this.dataSocket = dataSocket;
                this.InitializeSocket(dataSocket);
            }

            protected override Message DecodeMessage(ArraySegment<byte> data)
            {
                Message result = base.DecodeMessage(data);
                if (result != null)
                {
                    result.Headers.To = this.LocalAddress.Uri;
                    IPEndPoint remoteEndpoint = (IPEndPoint)this.dataSocket.RemoteEndPoint;
                    RemoteEndpointMessageProperty property = new RemoteEndpointMessageProperty(remoteEndpoint.Address.ToString(), remoteEndpoint.Port);
                    result.Properties.Add(RemoteEndpointMessageProperty.Name, property);
                }

                return result;
            }
        }

        class AcceptChannelAsyncResult : AsyncResult
        {
            TimeSpan timeout;
            SizedTcpDuplexChannelListener listener;
            IDuplexChannel channel;

            public AcceptChannelAsyncResult(TimeSpan timeout, SizedTcpDuplexChannelListener listener, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.timeout = timeout;
                this.listener = listener;

                IAsyncResult acceptResult = listener.listenSocket.BeginAccept(OnAccept, this);
                if (!acceptResult.CompletedSynchronously)
                {
                    return;
                }

                if (CompleteAccept(acceptResult))
                {
                    base.Complete(true);
                }
            }

            bool CompleteAccept(IAsyncResult result)
            {
                try
                {
                    Socket dataSocket = this.listener.listenSocket.EndAccept(result);
                    this.channel = new SizedTcpDuplexServerChannel(
                        this.listener.encoderFactory.Encoder,
                        this.listener.bufferManager,
                        new EndpointAddress(this.listener.uri),
                        dataSocket,
                        this.listener);
                }
                catch (ObjectDisposedException)
                {
                    this.channel = null;
                }

                return true;
            }

            static void OnAccept(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                AcceptChannelAsyncResult thisPtr = (AcceptChannelAsyncResult)result.AsyncState;

                Exception completionException = null;
                bool completeSelf = false;
                try
                {
                    completeSelf = thisPtr.CompleteAccept(result);
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

            public static IDuplexChannel End(IAsyncResult result)
            {
                AcceptChannelAsyncResult thisPtr = AsyncResult.End<AcceptChannelAsyncResult>(result);
                return thisPtr.channel;
            }
        }
    }
}
