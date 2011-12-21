using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.Net;
using System.ServiceModel.Channels;
using System.ServiceModel.Security.Tokens;

namespace Client
{
    class MyClientCredentialsSecurityTokenManager : ClientCredentialsSecurityTokenManager
    {
        public MyClientCredentialsSecurityTokenManager(ClientCredentials parent) : base(parent) { }
        public override SecurityTokenProvider CreateSecurityTokenProvider(SecurityTokenRequirement tokenRequirement)
        {
            if (tokenRequirement.KeyUsage == SecurityKeyUsage.Signature)
            {
                NetworkCredential token = null;
                ChannelParameterCollection obj;
                obj = (ChannelParameterCollection)tokenRequirement.Properties[
                    ServiceModelSecurityTokenRequirement.ChannelParametersCollectionProperty];
                token = obj[0] as NetworkCredential;
                if (tokenRequirement.TokenType == SecurityTokenTypes.UserName)
                {
                    return new MyUserNameSecurityTokenProvider(token);
                }
            }

            return base.CreateSecurityTokenProvider(tokenRequirement);
        }
    }
}
