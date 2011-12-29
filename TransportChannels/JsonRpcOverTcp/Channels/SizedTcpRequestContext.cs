using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Channels;

namespace JsonRpcOverTcp.Channels
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
            return this.replyChannel.BeginSendMessage(message, timeout, callback, state);
        }

        public override IAsyncResult BeginReply(Message message, AsyncCallback callback, object state)
        {
            return this.replyChannel.BeginSendMessage(message, callback, state);
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
            this.replyChannel.EndSendMessage(result);
        }

        public override void Reply(Message message, TimeSpan timeout)
        {
            this.replyChannel.SendMessage(message, timeout);
        }

        public override void Reply(Message message)
        {
            this.replyChannel.SendMessage(message, this.timeout);
        }

        public override Message RequestMessage
        {
            get { return this.requestMessage; }
        }
    }
}
