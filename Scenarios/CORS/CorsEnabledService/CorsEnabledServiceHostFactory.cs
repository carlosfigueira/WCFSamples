using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Web;

namespace CorsEnabledService
{
    class CorsEnabledServiceHostFactory : ServiceHostFactory
    {
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            return new CorsEnabledServiceHost(serviceType, baseAddresses);
        }
    }

    class CorsEnabledServiceHost : ServiceHost
    {
        Type contractType;
        public CorsEnabledServiceHost(Type serviceType, Uri[] baseAddresses)
            : base(serviceType, baseAddresses)
        {
            this.contractType = GetContractType(serviceType);
        }

        protected override void OnOpening()
        {
            ServiceEndpoint endpoint = this.AddServiceEndpoint(this.contractType, new WebHttpBinding(), "");

            List<OperationDescription> corsEnabledOperations = endpoint.Contract.Operations
                .Where(o => o.Behaviors.Find<CorsEnabledAttribute>() != null)
                .ToList();

            AddPreflightOperationSelectors(endpoint, corsEnabledOperations);

            endpoint.Behaviors.Add(new WebHttpBehavior());
            endpoint.Behaviors.Add(new EnableCorsEndpointBehavior());

            base.OnOpening();
        }

        private Type GetContractType(Type serviceType)
        {
            if (HasServiceContract(serviceType))
            {
                return serviceType;
            }

            Type[] possibleContractTypes = serviceType.GetInterfaces()
                .Where(i => HasServiceContract(i))
                .ToArray();

            switch (possibleContractTypes.Length)
            {
                case 0:
                    throw new InvalidOperationException("Service type " + serviceType.FullName + " does not implement any interface decorated with the ServiceContractAttribute.");
                case 1:
                    return possibleContractTypes[0];
                default:
                    throw new InvalidOperationException("Service type " + serviceType.FullName + " implements multiple interfaces decorated with the ServiceContractAttribute, not supported by this factory.");
            }
        }

        private static bool HasServiceContract(Type type)
        {
            return Attribute.IsDefined(type, typeof(ServiceContractAttribute), false);
        }

        private void AddPreflightOperationSelectors(ServiceEndpoint endpoint, List<OperationDescription> corsOperations)
        {
            foreach (var operation in corsOperations)
            {
                if (operation.Behaviors.Find<WebGetAttribute>() != null)
                {
                    // no need to add preflight operation for GET requests
                    continue;
                }

                if (operation.IsOneWay)
                {
                    // no support for 1-way messages
                    continue;
                }

                ContractDescription contract = operation.DeclaringContract;
                OperationDescription preflightOperation = new OperationDescription(operation.Name + CorsConstants.PreflightSuffix, contract);
                MessageDescription inputMessage = new MessageDescription(operation.Messages[0].Action + CorsConstants.PreflightSuffix, MessageDirection.Input);
                inputMessage.Body.Parts.Add(new MessagePartDescription("input", contract.Namespace) { Index = 0, Type = typeof(Message) });
                preflightOperation.Messages.Add(inputMessage);
                MessageDescription outputMessage = new MessageDescription(operation.Messages[1].Action + CorsConstants.PreflightSuffix, MessageDirection.Output);
                outputMessage.Body.ReturnValue = new MessagePartDescription(preflightOperation.Name + "Return", contract.Namespace) { Type = typeof(Message) };
                preflightOperation.Messages.Add(outputMessage);

                WebInvokeAttribute originalWia = operation.Behaviors.Find<WebInvokeAttribute>();
                WebInvokeAttribute wia = new WebInvokeAttribute();

                if (originalWia != null)
                {
                    if (originalWia.IsBodyStyleSetExplicitly)
                    {
                        wia.BodyStyle = originalWia.BodyStyle;
                    }

                    if (originalWia.IsRequestFormatSetExplicitly)
                    {
                        wia.RequestFormat = originalWia.RequestFormat;
                    }

                    if (originalWia.IsResponseFormatSetExplicitly)
                    {
                        wia.ResponseFormat = originalWia.ResponseFormat;
                    }

                    if (originalWia.UriTemplate != null)
                    {
                        wia.UriTemplate = originalWia.UriTemplate;
                    }
                }

                if (wia.UriTemplate == null)
                {
                    wia.UriTemplate = operation.Name;
                }

                wia.Method = "OPTIONS";

                preflightOperation.Behaviors.Add(wia);
                preflightOperation.Behaviors.Add(new DataContractSerializerOperationBehavior(preflightOperation));
                preflightOperation.Behaviors.Add(new PreflightOperationBehavior(preflightOperation));

                contract.Operations.Add(preflightOperation);
            }
        }
    }
}