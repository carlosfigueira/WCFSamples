using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace ParameterValidationWithSoap
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseAddress = "http://" + Environment.MachineName + ":8000/Service";
            ServiceHost host = new ServiceHost(typeof(ContactManager), new Uri(baseAddress));
            host.AddServiceEndpoint(typeof(IContactManager), new BasicHttpBinding(), "").Behaviors.Add(new ValidatingBehavior());
            host.Open();
            Console.WriteLine("Host opened");

            ChannelFactory<IContactManager> factory = new ChannelFactory<IContactManager>(new BasicHttpBinding(), new EndpointAddress(baseAddress));
            IContactManager proxy = factory.CreateChannel();

            Console.WriteLine("Test 1: adding a successful contract");
            string id = proxy.AddContact(new Contact { Name = "John Doe", Age = 30, Email = "john@doe.com" });
            Console.WriteLine("  ==> Contact id: {0}", id);
            Console.WriteLine();

            Console.WriteLine("Test 2: missing name");
            ExpectFaultOfString(delegate { proxy.AddContact(new Contact { Name = null, Age = 30, Email = "john@doe.com" }); });
            Console.WriteLine();

            Console.WriteLine("Test 3: empty name");
            ExpectFaultOfString(delegate { proxy.AddContact(new Contact { Name = "", Age = 30, Email = "john@doe.com" }); });
            Console.WriteLine();

            Console.WriteLine("Test 4: negative age");
            ExpectFaultOfString(delegate { proxy.AddContact(new Contact { Name = "John Doe", Age = -1, Email = "john@doe.com" }); });
            Console.WriteLine();

            Console.WriteLine("Test 5: very high age");
            ExpectFaultOfString(delegate { proxy.AddContact(new Contact { Name = "John Doe", Age = 400, Email = "john@doe.com" }); });
            Console.WriteLine();

            Console.WriteLine("Test 6: missing e-mail");
            ExpectFaultOfString(delegate { proxy.AddContact(new Contact { Name = "John Doe", Age = 30, Email = null }); });
            Console.WriteLine();

            Console.WriteLine("Test 7: invalid e-mail");
            ExpectFaultOfString(delegate { proxy.AddContact(new Contact { Name = "John Doe", Age = 30, Email = "abcdef" }); });
            Console.WriteLine();
        }

        static void ExpectFaultOfString(Action action)
        {
            try
            {
                action();
                Console.WriteLine("  ==> ERROR, expected exception not caught!");
            }
            catch (FaultException<string> fe)
            {
                Console.WriteLine("  ==> Caught expected exception: {0}", fe.Detail);
            }
        }
    }
}
