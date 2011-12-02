using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;

namespace JsonRpcOverTcp
{
    public class JsonRpcEndpointBehavior : IEndpointBehavior
    {
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            endpointDispatcher.ChannelDispatcher.ErrorHandlers.Add(new JsonRpcErrorHandler());
            endpointDispatcher.DispatchRuntime.OperationSelector = new JsonRpcOperationSelector(endpoint.Contract, endpointDispatcher.DispatchRuntime.OperationSelector);
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new JsonRpcMessageInspector());
            endpointDispatcher.ContractFilter = new MatchAllMessageFilter();
            foreach (OperationDescription operation in endpoint.Contract.Operations)
            {
                if (!JsonRpcHelpers.IsUntypedMessage(operation))
                {
                    DispatchOperation dispatchOperation = endpointDispatcher.DispatchRuntime.Operations[operation.Name];
                    dispatchOperation.Formatter = new JsonRpcMessageFormatter(operation);
                    dispatchOperation.DeserializeRequest = true;
                    dispatchOperation.SerializeReply = true;
                }
            }
        }

        public void Validate(ServiceEndpoint endpoint)
        {
            foreach (OperationDescription operation in endpoint.Contract.Operations)
            {
                if (operation.IsOneWay)
                {
                    throw new InvalidOperationException("One-way operations not supported in this implementation");
                }
            }
        }
    }
}
