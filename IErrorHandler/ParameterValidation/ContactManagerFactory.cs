using System;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Description;

namespace ParameterValidation
{
    public class ContactManagerFactory : ServiceHostFactory
    {
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            ServiceHost host = new ServiceHost(serviceType, baseAddresses);
            ServiceEndpoint endpoint = host.AddServiceEndpoint(serviceType, new WebHttpBinding(), "");
            endpoint.Behaviors.Add(new WebHttpWithValidationBehavior());
            return host;
        }
    }
}
