using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace SimulatedSessions
{
    class SharedSessionEndpointBehavior : IEndpointBehavior
    {
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            SharedSessionInstanceContextProvider extension = new SharedSessionInstanceContextProvider();
            endpointDispatcher.DispatchRuntime.InstanceContextProvider = extension;
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(extension);
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }
    }
}
