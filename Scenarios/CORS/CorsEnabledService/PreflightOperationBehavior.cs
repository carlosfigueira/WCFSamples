using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace CorsEnabledService
{
    class PreflightOperationBehavior : IOperationBehavior
    {
        private OperationDescription preflightOperation;

        public PreflightOperationBehavior(OperationDescription preflightOperation)
        {
            this.preflightOperation = preflightOperation;
        }

        public void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
        {
        }

        public void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
        {
            dispatchOperation.Invoker = new PreflightOperationInvoker(operationDescription.Messages[1].Action);
        }

        public void Validate(OperationDescription operationDescription)
        {
        }
    }
}