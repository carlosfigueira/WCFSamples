using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonRpcOverTcp.ServiceModel
{
    class JsonRpcMessageFormatter : IClientMessageFormatter, IDispatchMessageFormatter
    {
        private OperationDescription operation;
        private static int nextId = 0;

        public JsonRpcMessageFormatter(OperationDescription operation)
        {
            this.operation = operation;
        }

        public object DeserializeReply(Message message, object[] parameters)
        {
            JObject json = JsonRpcHelpers.DeserializeMessage(message);
            return JsonConvert.DeserializeObject(
                json[JsonRpcConstants.ResultKey].ToString(),
                this.operation.Messages[1].Body.ReturnValue.Type);
        }

        public Message SerializeRequest(MessageVersion messageVersion, object[] parameters)
        {
            JObject json = new JObject();
            json.Add(JsonRpcConstants.MethodKey, this.operation.Name);
            JArray methodParams = new JArray();
            json.Add(JsonRpcConstants.ParamsKey, methodParams);
            for (int i = 0; i < parameters.Length; i++)
            {
                methodParams.Add(null);
            }

            foreach (MessagePartDescription part in this.operation.Messages[0].Body.Parts)
            {
                object paramValue = parameters[part.Index];
                if (paramValue != null)
                {
                    methodParams[part.Index] = JToken.FromObject(paramValue);
                }
            }

            json.Add(JsonRpcConstants.IdKey, new JValue(Interlocked.Increment(ref nextId)));
            return JsonRpcHelpers.SerializeMessage(json, null);
        }

        public void DeserializeRequest(Message message, object[] parameters)
        {
            JObject json = JsonRpcHelpers.DeserializeMessage(message);
            JArray jsonParams = json[JsonRpcConstants.ParamsKey] as JArray;
            foreach (MessagePartDescription part in this.operation.Messages[0].Body.Parts)
            {
                int index = part.Index;
                if (jsonParams[index].Type != JTokenType.Null)
                {
                    parameters[index] = JsonConvert.DeserializeObject(
                        jsonParams[index].ToString(),
                        part.Type);
                }
            }
        }

        public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            JObject json = new JObject();
            json[JsonRpcConstants.ErrorKey] = null;
            json[JsonRpcConstants.ResultKey] = JToken.FromObject(result);
            return JsonRpcHelpers.SerializeMessage(json, null);
        }
    }
}
