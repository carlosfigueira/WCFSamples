using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Description;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace ParameterValidationWithSoap
{
    public class ValidatingBehavior : IEndpointBehavior
    {
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            endpointDispatcher.ChannelDispatcher.ErrorHandlers.Add(new ValidationAwareErrorHandler());
            foreach (DispatchOperation op in endpointDispatcher.DispatchRuntime.Operations)
            {
                op.ParameterInspectors.Add(new ValidatingParameterInspector());
            }
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }
    }
}
