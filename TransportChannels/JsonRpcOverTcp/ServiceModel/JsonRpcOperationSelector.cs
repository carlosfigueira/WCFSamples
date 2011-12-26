using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using Newtonsoft.Json.Linq;

namespace JsonRpcOverTcp.ServiceModel
{
    class JsonRpcOperationSelector : IDispatchOperationSelector
    {
        public string SelectOperation(ref Message message)
        {
            JObject json = JsonRpcHelpers.GetJObjectPreservingMessage(ref message);
            return json[JsonRpcConstants.MethodKey].Value<string>();
        }
    }
}
