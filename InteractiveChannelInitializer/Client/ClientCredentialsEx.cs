using System.IdentityModel.Selectors;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Client
{
    class ClientCredentialsEx : ClientCredentials
    {
        ClientForm clientForm;

        public ClientCredentialsEx(ClientForm clientForm)
            : base()
        {
            this.clientForm = clientForm;
        }

        public ClientCredentialsEx(ClientCredentialsEx other, ClientForm clientForm)
            : base(other)
        {
            this.clientForm = clientForm;
        }

        protected override ClientCredentials CloneCore()
        {
            return new ClientCredentialsEx(this, this.clientForm);
        }

        public override void ApplyClientBehavior(ServiceEndpoint serviceEndpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.InteractiveChannelInitializers.Add(new ShowCredentialsUI(this.clientForm));
            base.ApplyClientBehavior(serviceEndpoint, clientRuntime);
        }

        public override SecurityTokenManager CreateSecurityTokenManager()
        {
            return new MyClientCredentialsSecurityTokenManager(this);
        }
    }
}
