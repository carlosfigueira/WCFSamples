using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Net.Sockets;

namespace JsonRpcOverTcp
{
    class SizedTcpRequestContext : RequestContext
    {
        SizedTcpReplyChannel replyChannel;
        Message requestMessage;
        TimeSpan timeout;

        public SizedTcpRequestContext(SizedTcpReplyChannel replyChannel, Message requestMessage, TimeSpan timeout)
        {
            this.replyChannel = replyChannel;
            this.requestMessage = requestMessage;
            this.timeout = timeout;
        }

        public override void Abort()
        {
            this.replyChannel.Abort();
        }

        public override IAsyncResult BeginReply(Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new ReplyAsyncResult(message, timeout, this, callback, state);
        }

        public override IAsyncResult BeginReply(Message message, AsyncCallback callback, object state)
        {
            return this.BeginReply(message, this.timeout, callback, state);
        }

        public override void Close(TimeSpan timeout)
        {
            this.replyChannel.Close(timeout);
        }

        public override void Close()
        {
            this.replyChannel.Close();
        }

        public override void EndReply(IAsyncResult result)
        {
            ReplyAsyncResult.End(result);
        }

        public override void Reply(Message message, TimeSpan timeout)
        {
            this.replyChannel.Send(message, timeout);
        }

        public override void Reply(Message message)
        {
            this.Reply(message, this.timeout);
        }

        public override Message RequestMessage
        {
            get { return this.requestMessage; }
        }

        class ReplyAsyncResult : AsyncResult
        {
            SizedTcpRequestContext requestContext;

            public ReplyAsyncResult(Message message, TimeSpan timeout, SizedTcpRequestContext channel, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.requestContext = channel;

                IAsyncResult sendResult = this.requestContext.replyChannel.BeginSend(message, timeout, OnSend, this);

                if (!sendResult.CompletedSynchronously)
                {
                    return;
                }

                CompleteReply(sendResult);
                base.Complete(true);
            }

            void CompleteReply(IAsyncResult result)
            {
                try
                {
                    this.requestContext.replyChannel.EndSend(result);
                }
                catch (SocketException socketException)
                {
                    throw SizedTcpBaseChannel.ConvertSocketException(socketException, "Reply");
                }
            }

            static void OnSend(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                ReplyAsyncResult thisPtr = (ReplyAsyncResult)result.AsyncState;
                Exception completionException = null;
                try
                {
                    thisPtr.CompleteReply(result);
                }
                catch (Exception e)
                {
                    completionException = e;
                }

                thisPtr.Complete(false, completionException);
            }

            public static void End(IAsyncResult result)
            {
                AsyncResult.End<ReplyAsyncResult>(result);
            }
        }
    }
}
