using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;

namespace Server
{
    class MyPasswordValidator : UserNamePasswordValidator
    {
        public override void Validate(string userName, string password)
        {
            // Very secure password scheme
            if (userName != password)
            {
                throw new SecurityTokenException("Unauthorized");
            }
        }
    }
}
