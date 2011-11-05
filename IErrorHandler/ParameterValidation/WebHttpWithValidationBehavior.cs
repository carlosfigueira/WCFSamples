using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace ParameterValidation
{
    public class WebHttpWithValidationBehavior : WebHttpBehavior
    {
        protected override void AddServerErrorHandlers(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            int errorHandlerCount = endpointDispatcher.ChannelDispatcher.ErrorHandlers.Count;
            base.AddServerErrorHandlers(endpoint, endpointDispatcher);
            IErrorHandler webHttpErrorHandler = endpointDispatcher.ChannelDispatcher.ErrorHandlers[errorHandlerCount];
            endpointDispatcher.ChannelDispatcher.ErrorHandlers.RemoveAt(errorHandlerCount);
            ValidationAwareErrorHandler newHandler = new ValidationAwareErrorHandler(webHttpErrorHandler);
            endpointDispatcher.ChannelDispatcher.ErrorHandlers.Add(newHandler);
        }

        public override void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            base.ApplyDispatchBehavior(endpoint, endpointDispatcher);
            foreach (DispatchOperation operation in endpointDispatcher.DispatchRuntime.Operations)
            {
                operation.ParameterInspectors.Add(new ValidatingParameterInspector());
            }
        }
    }
}
