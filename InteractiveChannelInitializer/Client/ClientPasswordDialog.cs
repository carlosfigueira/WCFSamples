using System;
using System.Windows.Forms;

namespace Client
{
    public partial class ClientPasswordDialog : Form
    {
        private bool okSelected;
        private string userName;
        private string password;

        public ClientPasswordDialog()
        {
            InitializeComponent();
        }

        public bool OkSelected
        {
            get { return this.okSelected; }
        }

        public string UserName
        {
            get { return this.userName; }
        }

        public string Password
        {
            get { return this.password; }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.okSelected = false;
            this.userName = null;
            this.password = null;
            this.Close();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            this.okSelected = true;
            this.userName = this.txtUserName.Text;
            this.password = this.txtPassword.Text;
            this.Close();
        }
    }
}
