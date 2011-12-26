using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using Newtonsoft.Json.Linq;

namespace JsonRpcOverTcp.ServiceModel
{
    class JsonRpcErrorHandler : IErrorHandler
    {
        public bool HandleError(Exception error)
        {
            return true;
        }

        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {
            JObject json = new JObject();
            json.Add(JsonRpcConstants.ResultKey, null);
            JsonRpcException jsonException = error as JsonRpcException;
            if (jsonException != null)
            {
                json.Add(JsonRpcConstants.ErrorKey, jsonException.JsonException);
            }
            else
            {
                JObject exceptionJson = new JObject
                {
                    { "type", error.GetType().FullName },
                    { "message", error.Message },
                };
                JObject temp = exceptionJson;
                while (error.InnerException != null)
                {
                    error = error.InnerException;
                    JObject innerJson = new JObject
                    {
                        { "type", error.GetType().FullName },
                        { "message", error.Message },
                    };
                    temp["inner"] = innerJson;
                    temp = innerJson;
                }

                json.Add(JsonRpcConstants.ErrorKey, exceptionJson);
            }

            fault = JsonRpcHelpers.SerializeMessage(json, fault);
        }
    }
}
