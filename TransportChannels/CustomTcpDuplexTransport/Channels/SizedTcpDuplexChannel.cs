using System;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace CustomTcpDuplex.Channels
{
    abstract class SizedTcpDuplexChannel : SizedTcpBaseChannel, IDuplexChannel
    {
        Socket socket;
        Uri via;
        EndpointAddress localAddress;
        EndpointAddress remoteAddress;

        public SizedTcpDuplexChannel(
            MessageEncoder encoder,
            BufferManager bufferManager,
            ChannelManagerBase channelManager,
            EndpointAddress localAddress,
            EndpointAddress remoteAddress,
            Uri via)
            : base(encoder, bufferManager, channelManager)
        {
            this.via = via;
            this.remoteAddress = remoteAddress;
            this.localAddress = localAddress;
        }

        protected override void InitializeSocket(Socket socket)
        {
            this.socket = socket;
            base.InitializeSocket(socket);

            if (this.remoteAddress == null)
            {
                IPEndPoint remoteEndpoint = (IPEndPoint)socket.RemoteEndPoint;
                UriBuilder builder = new UriBuilder(
                    SizedTcpDuplexTransportBindingElement.SizedTcpScheme,
                    remoteEndpoint.Address.ToString(),
                    remoteEndpoint.Port);
                this.remoteAddress = new EndpointAddress(builder.Uri);
            }

            if (this.localAddress == null)
            {
                IPEndPoint localEndpoint = (IPEndPoint)socket.LocalEndPoint;
                UriBuilder builder = new UriBuilder(
                    SizedTcpDuplexTransportBindingElement.SizedTcpScheme,
                    localEndpoint.Address.ToString(),
                    localEndpoint.Port);
                this.localAddress = new EndpointAddress(builder.Uri);
            }
        }

        #region IInputChannel Members

        public IAsyncResult BeginReceive(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return base.BeginReceiveMessage(timeout, callback, state);
        }

        public IAsyncResult BeginReceive(AsyncCallback callback, object state)
        {
            return this.BeginReceive(this.DefaultReceiveTimeout, callback, state);
        }

        public IAsyncResult BeginTryReceive(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new TryReceiveAsyncResult(timeout, this, callback, state);
        }

        public IAsyncResult BeginWaitForMessage(TimeSpan timeout, AsyncCallback callback, object state)
        {
            throw new NotSupportedException("No peeking support");
        }

        public Message EndReceive(IAsyncResult result)
        {
            return base.EndReceiveMessage(result);
        }

        public bool EndTryReceive(IAsyncResult result, out Message message)
        {
            return TryReceiveAsyncResult.End(result, out message);
        }

        public bool EndWaitForMessage(IAsyncResult result)
        {
            throw new NotSupportedException("No peeking support");
        }

        public EndpointAddress LocalAddress
        {
            get { return this.localAddress; }
        }

        public Message Receive(TimeSpan timeout)
        {
            return base.ReceiveMessage(timeout);
        }

        public Message Receive()
        {
            return this.Receive(this.DefaultReceiveTimeout);
        }

        public bool TryReceive(TimeSpan timeout, out Message message)
        {
            try
            {
                message = base.ReceiveMessage(timeout);
                return true;
            }
            catch (TimeoutException)
            {
                message = null;
                return false;
            }
        }

        public bool WaitForMessage(TimeSpan timeout)
        {
            throw new NotSupportedException("No peeking support");
        }

        #endregion

        #region IOutputChannel Members

        public IAsyncResult BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            return base.BeginSendMessage(message, timeout, callback, state);
        }

        public IAsyncResult BeginSend(Message message, AsyncCallback callback, object state)
        {
            return this.BeginSend(message, this.DefaultSendTimeout, callback, state);
        }

        public void EndSend(IAsyncResult result)
        {
            base.EndSendMessage(result);
        }

        public EndpointAddress RemoteAddress
        {
            get { return this.remoteAddress; }
        }

        public void Send(Message message, TimeSpan timeout)
        {
            base.SendMessage(message, timeout);
        }

        public void Send(Message message)
        {
            this.Send(message, this.DefaultSendTimeout);
        }

        public Uri Via
        {
            get { return this.via; }
        }

        #endregion

        class TryReceiveAsyncResult : AsyncResult
        {
            SizedTcpDuplexChannel channel;
            bool receiveSuccess;
            Message message;

            public TryReceiveAsyncResult(TimeSpan timeout, SizedTcpDuplexChannel channel, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.channel = channel;

                bool completeSelf = true;
                if (!channel.IsDisposed)
                {
                    try
                    {
                        IAsyncResult beginReceiveRequestResult = this.channel.BeginReceive(timeout, OnReceive, this);
                        if (beginReceiveRequestResult.CompletedSynchronously)
                        {
                            CompleteReceive(beginReceiveRequestResult);
                        }
                        else
                        {
                            completeSelf = false;
                        }
                    }
                    catch (TimeoutException)
                    {
                    }
                }

                if (completeSelf)
                {
                    base.Complete(true);
                }
            }

            void CompleteReceive(IAsyncResult result)
            {
                this.message = this.channel.EndReceive(result);
                this.receiveSuccess = true;
            }

            static void OnReceive(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                TryReceiveAsyncResult thisPtr = (TryReceiveAsyncResult)result.AsyncState;
                Exception completionException = null;
                try
                {
                    thisPtr.CompleteReceive(result);
                }
                catch (TimeoutException)
                {
                }
                catch (Exception e)
                {
                    completionException = e;
                }

                thisPtr.Complete(false, completionException);
            }

            public static bool End(IAsyncResult result, out Message message)
            {
                try
                {
                    TryReceiveAsyncResult thisPtr = AsyncResult.End<TryReceiveAsyncResult>(result);
                    message = thisPtr.message;
                    return thisPtr.receiveSuccess;
                }
                catch (CommunicationException)
                {
                    message = null;
                    return false;
                }
            }
        }
    }
}
