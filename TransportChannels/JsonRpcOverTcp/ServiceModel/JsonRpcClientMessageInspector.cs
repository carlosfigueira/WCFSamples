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
    class JsonRpcClientMessageInspector : IClientMessageInspector
    {
        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            JObject json = GetJObjectPreservingMessage(ref reply);
            int replyId = json["id"].Value<int>();
            int requestId = (int)correlationState;
            if (replyId != requestId)
            {
                throw new CommunicationException("Reply does not correspond to the request!");
            }

            if (json[JsonRpcConstants.ErrorKey].Type != JTokenType.Null)
            {
                throw new CommunicationException("Error from the service: " + json[JsonRpcConstants.ErrorKey].ToString());
            }
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            JObject json = GetJObjectPreservingMessage(ref request);
            return json["id"].Value<int>();
        }

        static JObject GetJObjectPreservingMessage(ref Message message)
        {
            JObject json;
            if (message.Properties.ContainsKey(JsonRpcConstants.JObjectMessageProperty))
            {
                json = (JObject)message.Properties[JsonRpcConstants.JObjectMessageProperty];
            }
            else
            {
                json = JsonRpcHelpers.DeserializeMessage(message);
                message = JsonRpcHelpers.SerializeMessage(json, message);
            }

            return json;
        }
    }
}
