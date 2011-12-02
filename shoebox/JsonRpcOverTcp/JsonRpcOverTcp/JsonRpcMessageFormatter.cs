using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Json;
using System.Runtime.Serialization.Json;

namespace JsonRpcOverTcp
{
    class JsonRpcMessageFormatter : IDispatchMessageFormatter
    {
        OperationDescription operation;
        public JsonRpcMessageFormatter(OperationDescription operation)
        {
            this.operation = operation;
        }

        public void DeserializeRequest(Message message, object[] parameters)
        {
            JsonValue json = JsonRpcHelpers.DeserializeMessage(ref message);
            JsonArray requestParams = json.ValueOrDefault("params") as JsonArray;
            // skipping error checking here
            foreach (MessagePartDescription part in operation.Messages[0].Body.Parts)
            {
                parameters[part.Index] = requestParams[part.Index].ReadAs(part.Type);
            }
        }

        public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            JsonObject json = new JsonObject();
            json.Add(JsonRpcConstants.ResultKey, JsonValueExtensions.CreateFrom(result));
            json.Add(JsonRpcConstants.ErrorKey, null);
            return JsonRpcHelpers.SerializeMessage(json, null);
        }
    }
}
