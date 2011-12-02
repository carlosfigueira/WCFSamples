using System;
using System.Json;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace JsonRpcOverTcp
{
    class JsonRpcOperationSelector : IDispatchOperationSelector
    {
        private IDispatchOperationSelector originalOperationSelector;
        private ContractDescription contractDescription;

        public JsonRpcOperationSelector(ContractDescription contractDescription, IDispatchOperationSelector originalOperationSelector)
        {
            this.originalOperationSelector = originalOperationSelector;
            this.contractDescription = contractDescription;
        }

        public string SelectOperation(ref Message message)
        {
            JsonObject request = JsonRpcHelpers.DeserializeMessage(ref message);

            if (request.ContainsKey(JsonRpcConstants.MethodKey))
            {
                string methodName = request[JsonRpcConstants.MethodKey].ReadAs<string>();
                if (this.contractDescription.Operations.Find(methodName) != null)
                {
                    return methodName;
                }
                else
                {
                    return this.originalOperationSelector.SelectOperation(ref message);
                }
            }
            else
            {
                throw new ArgumentException("Incoming message does not contain the required \"" + JsonRpcConstants.MethodKey + "\" member");
            }
        }
    }
}
