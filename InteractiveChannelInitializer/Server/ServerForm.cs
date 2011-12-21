using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.IdentityModel.Policy;
using System.ServiceModel.Security;

namespace Server
{
    public partial class ServerForm : Form
    {
        ServiceHost host;
        public static readonly Uri BaseAddress = new Uri("http://" + Environment.MachineName + ":8000/Service");

        private static ServerForm instance;

        public ServerForm()
        {
            InitializeComponent();
            ServerForm.instance = this;
        }

        public static void UserCalled(string userName, string operation)
        {
            // Dispatch to the UI thread
            instance.lblConnectedUser.BeginInvoke(new Action(() =>
            {
                instance.lblConnectedUser.Text = userName + " (called " + operation + ")";
            }));
        }

        private static BasicHttpBinding GetBinding()
        {
            BasicHttpBinding result = new BasicHttpBinding();
            result.Security.Mode = BasicHttpSecurityMode.TransportCredentialOnly;
            result.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
            return result;
        }

        private void btnStartServer_Click(object sender, EventArgs e)
        {
            this.CreateAndOpenHost();
            this.btnStartServer.Enabled = false;
            this.btnStopServer.Enabled = true;
            this.lblStatus.Text = "Listening at " + BaseAddress;
        }

        private void CreateAndOpenHost()
        {
            host = new ServiceHost(typeof(Service));
            host.AddServiceEndpoint(typeof(ICalculator), GetBinding(), BaseAddress);
            List<IAuthorizationPolicy> policies = new List<IAuthorizationPolicy>();
            policies.Add(new CustomAuthorizationPolicy());
            host.Authorization.ExternalAuthorizationPolicies = policies.AsReadOnly();
            host.Authorization.PrincipalPermissionMode = PrincipalPermissionMode.Custom;

            host.Credentials.UserNameAuthentication.UserNamePasswordValidationMode = UserNamePasswordValidationMode.Custom;
            host.Credentials.UserNameAuthentication.CustomUserNamePasswordValidator = new MyPasswordValidator();

            host.Open();
        }

        private void btnStopServer_Click(object sender, EventArgs e)
        {
            this.host.Close();
            this.host = null;
            this.lblStatus.Text = "Closed";
            this.btnStopServer.Enabled = false;
            this.btnStartServer.Enabled = true;
        }
    }
}
