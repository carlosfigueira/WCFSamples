using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace HttpMethodOverrideOperationSelection
{
    class HttpOverrideOperationSelector : IDispatchOperationSelector
    {
        private IDispatchOperationSelector originalSelector;

        public HttpOverrideOperationSelector(IDispatchOperationSelector originalSelector)
        {
            this.originalSelector = originalSelector;
        }

        public string SelectOperation(ref Message message)
        {
            if (message.Properties.ContainsKey(HttpRequestMessageProperty.Name))
            {
                HttpRequestMessageProperty reqProp;
                reqProp = (HttpRequestMessageProperty)message.Properties[HttpRequestMessageProperty.Name];
                string httpMethodOverride = reqProp.Headers[HttpOverrideBehavior.HttpMethodOverrideHeaderName];
                if (!String.IsNullOrEmpty(httpMethodOverride))
                {
                    message.Properties[HttpOverrideBehavior.OriginalHttpMethodPropertyName] = reqProp.Method;
                    reqProp.Method = httpMethodOverride;
                }
            }

            return this.originalSelector.SelectOperation(ref message);
        }
    }
}
