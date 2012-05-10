using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace CorsEnabledService
{
    class EnableCorsEndpointBehavior : IEndpointBehavior
    {
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            List<OperationDescription> corsEnabledOperations = endpoint.Contract.Operations
                .Where(o => o.Behaviors.Find<CorsEnabledAttribute>() != null)
                .ToList();
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new CorsEnabledMessageInspector(corsEnabledOperations));
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }
    }
}