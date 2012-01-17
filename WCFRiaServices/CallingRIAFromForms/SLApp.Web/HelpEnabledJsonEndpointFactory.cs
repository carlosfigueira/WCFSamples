using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Description;
using System.ServiceModel.DomainServices.Hosting;
using System.ServiceModel.DomainServices.Server;
using Microsoft.ServiceModel.DomainServices.Hosting;

namespace SLApp.Web
{
    public class HelpEnabledJsonEndpointFactory : JsonEndpointFactory
    {
        public override IEnumerable<ServiceEndpoint> CreateEndpoints(DomainServiceDescription description, DomainServiceHost serviceHost)
        {
            List<ServiceEndpoint> endpoints = base.CreateEndpoints(description, serviceHost).ToList();
            foreach (ServiceEndpoint endpoint in endpoints)
            {
                WebHttpBehavior behavior = endpoint.Behaviors.Find<WebHttpBehavior>();
                if (behavior != null)
                {
                    behavior.HelpEnabled = true;
                }
            }

            return endpoints;
        }
    }
}