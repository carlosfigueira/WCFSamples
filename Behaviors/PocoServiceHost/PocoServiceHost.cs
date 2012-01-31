using System;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace PocoServiceHost
{
    public class PocoServiceHost : ServiceHost
    {
        public PocoServiceHost(Type serviceType, Uri baseAddress)
            : base(serviceType, baseAddress)
        {
        }

        protected override void InitializeRuntime()
        {
            this.Description.Behaviors.Insert(0, new PocoServiceBehavior());
            this.Description.Behaviors.Add(new ServiceMetadataBehavior { HttpGetEnabled = true });
            base.InitializeRuntime();
        }
    }
}
