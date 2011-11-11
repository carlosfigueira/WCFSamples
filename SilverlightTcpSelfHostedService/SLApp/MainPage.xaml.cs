using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

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
            client.EchoAsync("Hello world");
            this.AddToDebug("Called the service");
        }

        void client_EchoCompleted(object sender, ServiceReference1.EchoCompletedEventArgs e)
        {
            this.AddToDebug("Service replied: {0}", e.Result);
        }

        void AddToDebug(string text, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                text = string.Format(text, args);
            }

            this.txtDebug.Text = this.txtDebug.Text + text + Environment.NewLine;
        }
    }
}
