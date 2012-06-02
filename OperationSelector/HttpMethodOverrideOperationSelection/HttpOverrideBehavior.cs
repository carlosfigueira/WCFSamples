using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace HttpMethodOverrideOperationSelection
{
    public class HttpOverrideBehavior : IEndpointBehavior
    {
        public const string HttpMethodOverrideHeaderName = "X-HTTP-Method-Override";
        public const string OriginalHttpMethodPropertyName = "OriginalHttpMethod";

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            endpointDispatcher.DispatchRuntime.OperationSelector = new HttpOverrideOperationSelector(endpointDispatcher.DispatchRuntime.OperationSelector);
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }
    }
}
