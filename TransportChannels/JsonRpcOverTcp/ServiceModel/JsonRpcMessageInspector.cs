using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using System.ServiceModel;
using Newtonsoft.Json.Linq;

namespace JsonRpcOverTcp.ServiceModel
{
    class JsonRpcMessageInspector : IClientMessageInspector, IDispatchMessageInspector
    {
        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            JObject json = JsonRpcHelpers.GetJObjectPreservingMessage(ref reply);
            int replyId = json[JsonRpcConstants.IdKey].Value<int>();
            int requestId = (int)correlationState;
            if (replyId != requestId)
            {
                throw new JsonRpcException("id mismatch", "Reply does not correspond to the request!");
            }

            if (json[JsonRpcConstants.ErrorKey].Type != JTokenType.Null)
            {
                throw new JsonRpcException(json[JsonRpcConstants.ErrorKey]);
            }
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            JObject json = JsonRpcHelpers.GetJObjectPreservingMessage(ref request);
            return json[JsonRpcConstants.IdKey].Value<int>();
        }

        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            JObject json = JsonRpcHelpers.GetJObjectPreservingMessage(ref request);
            int requestId = json[JsonRpcConstants.IdKey].Value<int>();
            return requestId;
        }

        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            JObject json = JsonRpcHelpers.GetJObjectPreservingMessage(ref reply);
            json[JsonRpcConstants.IdKey] = (int)correlationState;
            reply = JsonRpcHelpers.SerializeMessage(json, reply);
        }
    }
}
