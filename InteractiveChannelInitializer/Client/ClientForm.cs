using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace Client
{
    public partial class ClientForm : Form
    {
        enum OperationType
        {
            Add, Subtract, Multiply, Divide
        }

        ServiceReference1.CalculatorClient client;

        public ClientForm()
        {
            InitializeComponent();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            this.CallService(OperationType.Add);
        }

        private void btnSubtract_Click(object sender, EventArgs e)
        {
            this.CallService(OperationType.Subtract);
        }

        private void btnMultiply_Click(object sender, EventArgs e)
        {
            this.CallService(OperationType.Multiply);
        }

        private void btnDivide_Click(object sender, EventArgs e)
        {
            this.CallService(OperationType.Divide);
        }

        private void InitializeClient()
        {
            if (this.client == null)
            {
                this.client = new ServiceReference1.CalculatorClient();
                BasicHttpBinding binding = this.client.Endpoint.Binding as BasicHttpBinding;
                binding.Security.Mode = BasicHttpSecurityMode.TransportCredentialOnly;
                binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;

                //client.ClientCredentials.UserName.UserName = "John Doe";
                //client.ClientCredentials.UserName.Password = "John Doe";

                this.client.Endpoint.Behaviors.Remove<ClientCredentials>();
                this.client.Endpoint.Behaviors.Add(new ClientCredentialsEx(this));
            }
        }

        private void CallService(OperationType operation)
        {
            int x = int.Parse(this.txtX.Text);
            int y = int.Parse(this.txtY.Text);

            this.InitializeClient();

            AsyncCallback callback = new AsyncCallback(delegate(IAsyncResult ar)
            {
                int r = 0;
                switch (operation)
                {
                    case OperationType.Add:
                        r = this.client.EndAdd(ar);
                        break;
                    case OperationType.Subtract:
                        r = this.client.EndSubtract(ar);
                        break;
                    case OperationType.Multiply:
                        r = this.client.EndMultiply(ar);
                        break;
                    case OperationType.Divide:
                        r = this.client.EndDivide(ar);
                        break;
                }

                this.txtResult.BeginInvoke(new Action(() => this.txtResult.Text = r.ToString(CultureInfo.InvariantCulture)));
            });

            switch (operation)
            {
                case OperationType.Add:
                    this.client.BeginAdd(x, y, callback, null);
                    break;
                case OperationType.Subtract:
                    this.client.BeginSubtract(x, y, callback, null);
                    break;
                case OperationType.Multiply:
                    this.client.BeginMultiply(x, y, callback, null);
                    break;
                case OperationType.Divide:
                    this.client.BeginDivide(x, y, callback, null);
                    break;
            }
            //int result = 0;
            //switch (operation)
            //{
            //    case OperationType.Add:
            //        result = client.Add(x, y);
            //        break;
            //    case OperationType.Subtract:
            //        result = client.Subtract(x, y);
            //        break;
            //    case OperationType.Multiply:
            //        result = client.Multiply(x, y);
            //        break;
            //    case OperationType.Divide:
            //        result = client.Divide(x, y);
            //        break;
            //}

            //this.txtResult.Text = result.ToString(CultureInfo.InvariantCulture);
        }
    }
}
