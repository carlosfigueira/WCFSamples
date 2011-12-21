using System.Security.Principal;

namespace Server
{
    class CustomPrincipal : IPrincipal
    {
        IIdentity identity;
        public CustomPrincipal(IIdentity identity)
        {
            this.identity = identity;
        }

        public IIdentity Identity
        {
            get { return this.identity; }
        }

        public bool IsInRole(string role)
        {
            return true;
        }
    }
}
