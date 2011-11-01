using System;
using System.Windows;
using System.Windows.Controls;

namespace SLApp
{
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ServiceReference1.TestClient client = new ServiceReference1.TestClient();
            client.EchoCompleted += new EventHandler<ServiceReference1.EchoCompletedEventArgs>(client_EchoCompleted);
            client.EchoAsync("Hello");
            this.txtDebug.Text = this.txtDebug.Text + "Called the service" + Environment.NewLine;
        }

        void client_EchoCompleted(object sender, ServiceReference1.EchoCompletedEventArgs e)
        {
            this.txtDebug.Text = this.txtDebug.Text + "Server response: " + e.Result + Environment.NewLine;
        }
    }
}
