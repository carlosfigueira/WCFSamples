using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Net.Sockets;
using System.Collections.ObjectModel;
using System.Net;

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
            throw new NotImplementedException();
        }

        protected override IAsyncResult OnBeginAcceptChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        protected override IReplyChannel OnEndAcceptChannel(IAsyncResult result)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        protected override void OnClose(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        protected override void OnEndClose(IAsyncResult result)
        {
            throw new NotImplementedException();
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            throw new NotImplementedException();
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            throw new NotImplementedException();
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
    }
}
