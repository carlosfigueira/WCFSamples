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
using SLApp.Web;
using System.ServiceModel.Channels;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace SLApp
{
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();
        }

        IRestService CreateProxy()
        {
            CustomBinding binding = new CustomBinding(
                new WebMessageEncodingBindingElement(),
                new HttpTransportBindingElement { ManualAddressing = true });
            string address = GetServiceAddress();
            ChannelFactory<IRestService> factory = new ChannelFactory<IRestService>(binding, new EndpointAddress(address));
            factory.Endpoint.Behaviors.Add(new WebHttpBehaviorWithJson());
            IRestService proxy = factory.CreateChannel();
            ((IClientChannel)proxy).Closed += delegate { factory.Close(); };
            return proxy;
        }

        private string GetServiceAddress()
        {
            string address = Application.Current.Host.Source.ToString();
            address = address.Substring(0, address.LastIndexOf('/'));
            if (address.EndsWith("/bin", StringComparison.OrdinalIgnoreCase) || address.EndsWith("/clientbin", StringComparison.OrdinalIgnoreCase))
            {
                address = address.Substring(0, address.LastIndexOf('/'));
            }
            return address + "/RestService.svc";
        }

        void CloseCallback(IAsyncResult asyncResult)
        {
            IRestService proxy = (IRestService)asyncResult.AsyncState;
            ((IClientChannel)proxy).EndClose(asyncResult);
        }

        private void btnAddJson_Click(object sender, RoutedEventArgs e)
        {
            IRestService proxy = CreateProxy();
            int x = int.Parse(this.txtX.Text);
            int y = int.Parse(this.txtY.Text);
            proxy.BeginAddGetJson(x, y, new AsyncCallback(AddJsonCompleted), proxy);
        }

        private void btnAddXml_Click(object sender, RoutedEventArgs e)
        {
            IRestService proxy = CreateProxy();
            int x = int.Parse(this.txtX.Text);
            int y = int.Parse(this.txtY.Text);
            proxy.BeginAddGetXml(x, y, new AsyncCallback(AddXmlCompleted), proxy);
        }

        private void btnSubtractJson_Click(object sender, RoutedEventArgs e)
        {
            IRestService proxy = CreateProxy();
            int x = int.Parse(this.txtX.Text);
            int y = int.Parse(this.txtY.Text);
            proxy.BeginSubtractPostJson(x, y, new AsyncCallback(SubtractJsonCompleted), proxy);
        }

        private void btnSubtractXml_Click(object sender, RoutedEventArgs e)
        {
            IRestService proxy = CreateProxy();
            int x = int.Parse(this.txtX.Text);
            int y = int.Parse(this.txtY.Text);
            proxy.BeginSubtractPostXml(x, y, new AsyncCallback(SubtractXmlCompleted), proxy);
        }

        void AddJsonCompleted(IAsyncResult asyncResult)
        {
            IRestService proxy = (IRestService)asyncResult.AsyncState;
            int result = proxy.EndAddGetJson(asyncResult);
            SetResult("Add (JSON) result: {0}", result);
            ((IClientChannel)proxy).BeginClose(new AsyncCallback(CloseCallback), proxy);
        }

        void AddXmlCompleted(IAsyncResult asyncResult)
        {
            IRestService proxy = (IRestService)asyncResult.AsyncState;
            int result = proxy.EndAddGetXml(asyncResult);
            SetResult("Add (XML) result: {0}", result);
            ((IClientChannel)proxy).BeginClose(new AsyncCallback(CloseCallback), proxy);
        }

        void SubtractJsonCompleted(IAsyncResult asyncResult)
        {
            IRestService proxy = (IRestService)asyncResult.AsyncState;
            int result = proxy.EndSubtractPostJson(asyncResult);
            SetResult("Subtract (JSON) result: {0}", result);
            ((IClientChannel)proxy).BeginClose(new AsyncCallback(CloseCallback), proxy);
        }

        void SubtractXmlCompleted(IAsyncResult asyncResult)
        {
            IRestService proxy = (IRestService)asyncResult.AsyncState;
            int result = proxy.EndSubtractPostXml(asyncResult);
            SetResult("Subtract (XML) result: {0}", result);
            ((IClientChannel)proxy).BeginClose(new AsyncCallback(CloseCallback), proxy);
        }

        void ReverseTextCompleted(IAsyncResult asyncResult)
        {
            IRestService proxy = (IRestService)asyncResult.AsyncState;
            string result = proxy.EndReverseUriTemplateGet(asyncResult);
            SetResult("Reverse (GET) result: {0}", result);
            ((IClientChannel)proxy).BeginClose(new AsyncCallback(CloseCallback), proxy);
        }

        void SetResult(string text, params object[] args)
        {
            string result = text;
            if (args != null && args.Length > 0)
            {
                result = String.Format(text, args);
            }

            this.Dispatcher.BeginInvoke(() => this.txtResult.Text = result);
        }

        private void btnReverseText_Click(object sender, RoutedEventArgs e)
        {
            IRestService proxy = CreateProxy();
            proxy.BeginReverseUriTemplateGet(this.txtTextInput.Text, new AsyncCallback(ReverseTextCompleted), proxy);
        }

        private void btnCreatePerson_Click(object sender, RoutedEventArgs e)
        {
            IRestService proxy = CreateProxy();
            string name = this.txtName.Text;
            int age = int.Parse(this.txtAge.Text);
            DateTime dob = new DateTime(1980, 12, 15);
            proxy.BeginCreatePerson(name, age, dob, new AsyncCallback(CreatePersonCompleted), proxy);
        }

        private void btnCreateOrder_Click(object sender, RoutedEventArgs e)
        {
            IRestService proxy = CreateProxy();
            int numberOfItems = int.Parse(this.txtNumberOfItems.Text);
            Order order = new Order();
            order.Processed = true;
            order.Items = new List<OrderItem>();
            for (int i = 0; i < numberOfItems; i++)
            {
                OrderItem item = new OrderItem();
                item.Amount = i + 1;
                item.Product = new Product
                {
                    ID = i,
                    Name = "Product " + i,
                };
                order.Items.Add(item);
            }
            proxy.BeginCreateOrder(order, new AsyncCallback(CreateOrderCompleted), proxy);
        }

        void CreatePersonCompleted(IAsyncResult asyncResult)
        {
            IRestService proxy = (IRestService)asyncResult.AsyncState;
            Person result = proxy.EndCreatePerson(asyncResult);
            SetResult("CreatePerson result: {0}", result);
            ((IClientChannel)proxy).BeginClose(new AsyncCallback(CloseCallback), proxy);
        }

        void CreateOrderCompleted(IAsyncResult asyncResult)
        {
            IRestService proxy = (IRestService)asyncResult.AsyncState;
            int orderId = proxy.EndCreateOrder(asyncResult);
            SetResult("CreateOrder result: {0}", orderId);
            ((IClientChannel)proxy).BeginClose(new AsyncCallback(CloseCallback), proxy);
        }
    }
}
