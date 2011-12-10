using System;
using Newtonsoft.Json.Linq;

namespace JsonRpcOverTcp.ServiceModel
{
    public class JsonRpcException : Exception
    {
        public JToken JsonException
        {
            get;
            private set;
        }

        public JsonRpcException(JToken json)
            : base()
        {
            this.JsonException = json;
        }

        public JsonRpcException(JToken json, string message)
            : base(message)
        {
            this.JsonException = json;
        }

        public JsonRpcException(JToken json, string message, Exception innerException)
            : base(message, innerException)
        {
            this.JsonException = json;
        }
    }
}
