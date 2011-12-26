using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Channels;
using System.Net.Sockets;
using System.ServiceModel;
using System.Net;

namespace JsonRpcOverTcp.Channels
{
    class SizedTcpReplyChannel : SizedTcpBaseChannel, IReplyChannel
    {
        Uri localAddress;
        Socket socket;

        public SizedTcpReplyChannel(MessageEncoder encoder, BufferManager bufferManager, Uri localAddress, Socket socket, ChannelManagerBase channelManager)
            : base(encoder, bufferManager, channelManager)
        {
            this.localAddress = localAddress;
            this.socket = socket;
            this.InitializeSocket(socket);
        }

        public IAsyncResult BeginReceiveRequest(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new ReceiveRequestAsyncResult(timeout, this, callback, state);
        }

        public IAsyncResult BeginReceiveRequest(AsyncCallback callback, object state)
        {
            return this.BeginReceiveRequest(this.DefaultReceiveTimeout, callback, state);
        }

        public IAsyncResult BeginTryReceiveRequest(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new TryReceiveRequestAsyncResult(timeout, this, callback, state);
        }

        public IAsyncResult BeginWaitForRequest(TimeSpan timeout, AsyncCallback callback, object state)
        {
            throw new NotSupportedException("No peeking support");
        }

        public RequestContext EndReceiveRequest(IAsyncResult result)
        {
            return ReceiveRequestAsyncResult.End(result);
        }

        public bool EndTryReceiveRequest(IAsyncResult result, out RequestContext context)
        {
            return TryReceiveRequestAsyncResult.End(result, out context);
        }

        public bool EndWaitForRequest(IAsyncResult result)
        {
            throw new NotSupportedException("No peeking support");
        }

        public EndpointAddress LocalAddress
        {
            get { return new EndpointAddress(this.localAddress); }
        }

        public RequestContext ReceiveRequest(TimeSpan timeout)
        {
            Message request = this.ReceiveMessage(timeout);
            return new SizedTcpRequestContext(this, request, timeout);
        }

        public RequestContext ReceiveRequest()
        {
            return this.ReceiveRequest(this.DefaultReceiveTimeout);
        }

        public bool TryReceiveRequest(TimeSpan timeout, out RequestContext context)
        {
            try
            {
                context = this.ReceiveRequest(timeout);
                return true;
            }
            catch (TimeoutException)
            {
                context = null;
                return false;
            }
        }

        public bool WaitForRequest(TimeSpan timeout)
        {
            throw new NotSupportedException("No peeking support");
        }

        protected override Message DecodeMessage(ArraySegment<byte> data)
        {
            Message result = base.DecodeMessage(data);
            if (result != null)
            {
                result.Headers.To = this.localAddress;
                IPEndPoint remoteEndpoint = (IPEndPoint)this.socket.RemoteEndPoint;
                RemoteEndpointMessageProperty property = new RemoteEndpointMessageProperty(remoteEndpoint.Address.ToString(), remoteEndpoint.Port);
                result.Properties.Add(RemoteEndpointMessageProperty.Name, property);
            }

            return result;
        }

        class ReceiveRequestAsyncResult : AsyncResult
        {
            SizedTcpReplyChannel channel;
            RequestContext requestContext;
            TimeSpan timeout;

            public ReceiveRequestAsyncResult(TimeSpan timeout, SizedTcpReplyChannel channel, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.channel = channel;
                this.timeout = timeout;

                if (!channel.IsDisposed)
                {
                    IAsyncResult receiveMessageResult = channel.BeginReceiveMessage(timeout, OnReceiveMessage, this);
                    if (!receiveMessageResult.CompletedSynchronously)
                    {
                        return;
                    }

                    CompleteReceiveMessage(receiveMessageResult);
                }

                base.Complete(true);
            }

            void CompleteReceiveMessage(IAsyncResult result)
            {
                try
                {
                    Message message = this.channel.EndReceiveMessage(result);
                    this.requestContext = new SizedTcpRequestContext(this.channel, message, this.timeout);
                }
                catch (CommunicationException e)
                {
                    this.Complete(result.CompletedSynchronously, e);
                }
            }

            static void OnReceiveMessage(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                ReceiveRequestAsyncResult thisPtr = (ReceiveRequestAsyncResult)result.AsyncState;

                Exception completionException = null;
                try
                {
                    thisPtr.CompleteReceiveMessage(result);
                }
                catch (Exception e)
                {
                    completionException = e;
                }

                thisPtr.Complete(false, completionException);
            }

            public static RequestContext End(IAsyncResult result)
            {
                ReceiveRequestAsyncResult thisPtr = AsyncResult.End<ReceiveRequestAsyncResult>(result);
                return thisPtr.requestContext;
            }
        }

        class TryReceiveRequestAsyncResult : AsyncResult
        {
            SizedTcpReplyChannel channel;
            bool receiveSuccess;
            RequestContext requestContext;

            public TryReceiveRequestAsyncResult(TimeSpan timeout, SizedTcpReplyChannel channel, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.channel = channel;

                bool completeSelf = true;
                if (!channel.IsDisposed)
                {
                    try
                    {
                        IAsyncResult beginReceiveRequestResult = this.channel.BeginReceiveRequest(timeout, OnReceiveRequest, this);
                        if (beginReceiveRequestResult.CompletedSynchronously)
                        {
                            CompleteReceiveRequest(beginReceiveRequestResult);
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

            void CompleteReceiveRequest(IAsyncResult result)
            {
                this.requestContext = this.channel.EndReceiveRequest(result);
                this.receiveSuccess = true;
            }

            static void OnReceiveRequest(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                TryReceiveRequestAsyncResult thisPtr = (TryReceiveRequestAsyncResult)result.AsyncState;
                Exception completionException = null;
                try
                {
                    thisPtr.CompleteReceiveRequest(result);
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

            public static bool End(IAsyncResult result, out RequestContext requestContext)
            {
                try
                {
                    TryReceiveRequestAsyncResult thisPtr = AsyncResult.End<TryReceiveRequestAsyncResult>(result);
                    requestContext = thisPtr.requestContext;
                    return thisPtr.receiveSuccess;
                }
                catch (CommunicationException)
                {
                    requestContext = null;
                    return false;
                }
            }
        }
    }
}
