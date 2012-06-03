using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace PricingService
{
    public class InstanceProviderBehaviorAttribute : Attribute, IServiceBehavior
    {
        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, System.Collections.ObjectModel.Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            foreach (ChannelDispatcher cd in serviceHostBase.ChannelDispatchers)
            {
                foreach (EndpointDispatcher ed in cd.Endpoints)
                {
                    if (!ed.IsSystemEndpoint)
                    {
                        ed.DispatchRuntime.InstanceProvider = new ServiceInstanceProvider();
                    }
                }
            }
        }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
        }

        class ServiceInstanceProvider : IInstanceProvider
        {
            public object GetInstance(InstanceContext instanceContext, Message message)
            {
                IProductRepository repository = ProductRepositoryFactory.Instance.CreateProductRepository();
                return new PricingService(repository);
            }

            public object GetInstance(InstanceContext instanceContext)
            {
                return this.GetInstance(instanceContext, null);
            }

            public void ReleaseInstance(InstanceContext instanceContext, object instance)
            {
            }
        }
    }
}
