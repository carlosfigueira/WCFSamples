using System;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.Net;

namespace Client
{
    class MyUserNameSecurityTokenProvider : SecurityTokenProvider
    {
        private NetworkCredential credentials;

        public MyUserNameSecurityTokenProvider(NetworkCredential credentials)
        {
            this.credentials = credentials;
        }

        protected override SecurityToken GetTokenCore(TimeSpan timeout)
        {
            return new UserNameSecurityToken(this.credentials.UserName, this.credentials.Password);
        }
    }
}
