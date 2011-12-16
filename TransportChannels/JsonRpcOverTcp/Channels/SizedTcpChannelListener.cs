using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel.Channels;

namespace JsonRpcOverTcp.Channels
{
    class SizedTcpChannelListener : ChannelListenerBase<IReplyChannel>
    {
        BufferManager bufferManager;
        MessageEncoderFactory encoderFactory;
        Socket listenSocket;
        Uri uri;

        public SizedTcpChannelListener(SizedTcpTransportBindingElement bindingElement, BindingContext context)
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
                if (!(messageEncoderBindingElements[0] is ByteStreamMessageEncodingBindingElement))
                {
                    throw new InvalidOperationException("This transport must be used with the ByteStreamMessageEncodingBindingElement.");
                }

                this.encoderFactory = messageEncoderBindingElements[0].CreateMessageEncoderFactory();
            }
            else
            {
                this.encoderFactory = new ByteStreamMessageEncodingBindingElement().CreateMessageEncoderFactory();
            }

            this.uri = new Uri(context.ListenUriBaseAddress, context.ListenUriRelativeAddress);
        }

        protected override IReplyChannel OnAcceptChannel(TimeSpan timeout)
        {
            try
            {
                Socket dataSocket = listenSocket.Accept();
                return new SizedTcpReplyChannel(this.encoderFactory.Encoder, this.bufferManager, this.uri, dataSocket, this);
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

        protected override IReplyChannel OnEndAcceptChannel(IAsyncResult result)
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

        class AcceptChannelAsyncResult : AsyncResult
        {
            TimeSpan timeout;
            SizedTcpChannelListener listener;
            IReplyChannel channel;

            public AcceptChannelAsyncResult(TimeSpan timeout, SizedTcpChannelListener listener, AsyncCallback callback, object state)
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
                    this.channel = new SizedTcpReplyChannel(
                        this.listener.encoderFactory.Encoder,
                        this.listener.bufferManager,
                        this.listener.uri,
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

            public static IReplyChannel End(IAsyncResult result)
            {
                AcceptChannelAsyncResult thisPtr = AsyncResult.End<AcceptChannelAsyncResult>(result);
                return thisPtr.channel;
            }
        }
    }
}
