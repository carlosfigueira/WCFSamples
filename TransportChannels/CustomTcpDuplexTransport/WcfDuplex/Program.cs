using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using CustomTcpDuplex.Channels;

namespace WcfDuplex
{
    class Program
    {
        class ClientCallback : ITestCallback
        {
            public void OnHello(string text)
            {
                Console.WriteLine("[client received] {0}", text);
            }
        }

        static void Main(string[] args)
        {
            string baseAddress = SizedTcpDuplexTransportBindingElement.SizedTcpScheme + "://localhost:8000";
            ServiceHost host = new ServiceHost(typeof(Service), new Uri(baseAddress));
            Binding binding = new CustomBinding(new SizedTcpDuplexTransportBindingElement());
            ServiceEndpoint endpoint = host.AddServiceEndpoint(typeof(ITest), binding, "");
            host.Open();
            Console.WriteLine("Host opened");

            InstanceContext instanceContext = new InstanceContext(new ClientCallback());
            EndpointAddress endpointAddress = new EndpointAddress(baseAddress);
            DuplexChannelFactory<ITest> factory = new DuplexChannelFactory<ITest>(instanceContext, binding, endpointAddress);
            ITest proxy = factory.CreateChannel();

            proxy.Hello("John Doe");

            Console.WriteLine("Press ENTER to close");
            Console.ReadLine();

            ((IClientChannel)proxy).Close();
            factory.Close();
            host.Close();
        }
    }
}
