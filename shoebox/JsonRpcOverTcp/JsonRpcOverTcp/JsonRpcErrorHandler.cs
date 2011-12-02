using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using System.Json;

namespace JsonRpcOverTcp
{
    class JsonRpcErrorHandler : IErrorHandler
    {
        public bool HandleError(Exception error)
        {
            return true;
        }

        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {
            JsonObject json = new JsonObject();
            json.Add(JsonRpcConstants.ResultKey, null);
            JsonObject exceptionJson = new JsonObject
            {
                { "type", error.GetType().FullName },
                { "message", error.Message },
            };
            JsonObject temp = exceptionJson;
            while (error.InnerException != null)
            {
                error = error.InnerException;
                JsonObject innerJson = new JsonObject
                {
                    { "type", error.GetType().FullName },
                    { "message", error.Message },
                };
                temp["inner"] = innerJson;
                temp = innerJson;
            }

            json.Add(JsonRpcConstants.ErrorKey, exceptionJson);
            fault = JsonRpcHelpers.SerializeMessage(json, fault);
        }
    }
}
