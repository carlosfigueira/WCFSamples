using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Dispatcher;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Json;

namespace JsonRpcOverTcp
{
    class JsonRpcMessageInspector : IDispatchMessageInspector
    {
        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            JsonObject json = JsonRpcHelpers.DeserializeMessage(ref request);
            if (!json.ContainsKey(JsonRpcConstants.IdKey))
            {
                throw new ArgumentException("Request must contain a request id");
            }

            return json[JsonRpcConstants.IdKey].ReadAs<int>();
        }

        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            int requestId = (int)correlationState;
            reply.Properties.Add(JsonRpcConstants.RequestIdMessageProperty, requestId);
            JsonObject json = JsonRpcHelpers.DeserializeMessage(ref reply);
            json.Add(JsonRpcConstants.IdKey, requestId);
            reply = JsonRpcHelpers.SerializeMessage(json, reply);
        }
    }
}
