using System;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace TrackingClients
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseAddress = "http://" + Environment.MachineName + ":8000/Service";
            ServiceHost host = new ServiceHost(typeof(StackCalculator), new Uri(baseAddress));
            WSHttpBinding binding = new WSHttpBinding(SecurityMode.None);
            binding.ReliableSession.Enabled = true;
            ServiceEndpoint endpoint = host.AddServiceEndpoint(typeof(IStackCalculator), binding, "");
            endpoint.Behaviors.Add(new ClientTrackerEndpointBehavior());
            host.Open();
            Console.WriteLine("Host opened");

            ChannelFactory<IStackCalculator> factory = new ChannelFactory<IStackCalculator>(binding, new EndpointAddress(baseAddress));
            IStackCalculator proxy1 = factory.CreateChannel();
            Console.WriteLine("Created first client");
            proxy1.Enter(5);
            proxy1.Enter(8);
            proxy1.Multiply();
            Console.WriteLine();

            IStackCalculator proxy2 = factory.CreateChannel();
            Console.WriteLine("Created second channel");
            proxy2.Enter(5);
            proxy2.Enter(2);
            proxy2.Divide();
            Console.WriteLine();

            Console.WriteLine("Disconnecting the first client");
            ((IClientChannel)proxy1).Close();
            Console.WriteLine();

            Console.WriteLine("Using the second proxy again");
            proxy2.Enter(10);
            proxy2.Multiply();
            Console.WriteLine();

            Console.WriteLine("Closing the second client");
            ((IClientChannel)proxy2).Close();

            factory.Close();
            host.Close();
        }
    }
}
