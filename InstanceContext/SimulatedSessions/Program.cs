using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Threading;

namespace SimulatedSessions
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseAddress = "http://" + Environment.MachineName + ":8000/Service";
            ServiceHost host = new ServiceHost(typeof(StackCalculator), new Uri(baseAddress));
            var binding = new BasicHttpBinding();
            ServiceEndpoint endpoint = host.AddServiceEndpoint(typeof(IStackCalculator), binding, "");
            endpoint.Behaviors.Add(new SharedSessionEndpointBehavior());
            host.Open();
            Console.WriteLine("Host opened");

            ChannelFactory<IStackCalculator> factory = new ChannelFactory<IStackCalculator>(binding, new EndpointAddress(baseAddress));
            IStackCalculator proxy = factory.CreateChannel();

            using (new OperationContextScope((IContextChannel)proxy))
            {
                OperationContext.Current.OutgoingMessageHeaders.Add(
                    MessageHeader.CreateHeader(
                        Constants.HeaderName,
                        Constants.HeaderNamespace,
                        "abcdef"));

                proxy.Enter(2);
                proxy.Enter(3);
                proxy.Add();
                proxy.Enter(4);
                proxy.Subtract();
                Console.WriteLine();
            }

            ((IClientChannel)proxy).Close();

            Console.WriteLine("Same header name...");
            proxy = factory.CreateChannel();
            using (new OperationContextScope((IContextChannel)proxy))
            {
                OperationContext.Current.OutgoingMessageHeaders.Add(
                    MessageHeader.CreateHeader(
                        Constants.HeaderName,
                        Constants.HeaderNamespace,
                        "abcdef"));

                proxy.Enter(5);
                proxy.Enter(6);
                proxy.Add();
                proxy.Enter(7);
                proxy.Subtract();
                Console.WriteLine();
            }

            ((IClientChannel)proxy).Close();

            Console.WriteLine("Now with a different header");
            proxy = factory.CreateChannel();
            using (new OperationContextScope((IContextChannel)proxy))
            {
                OperationContext.Current.OutgoingMessageHeaders.Add(
                    MessageHeader.CreateHeader(
                        Constants.HeaderName,
                        Constants.HeaderNamespace,
                        "other"));

                proxy.Enter(8);
                proxy.Enter(9);
                proxy.Add();
                proxy.Enter(10);
                proxy.Subtract();
                Console.WriteLine();
            }

            ((IClientChannel)proxy).Close();

            Console.WriteLine("Back to the first header...");
            proxy = factory.CreateChannel();
            using (new OperationContextScope((IContextChannel)proxy))
            {
                OperationContext.Current.OutgoingMessageHeaders.Add(
                    MessageHeader.CreateHeader(
                        Constants.HeaderName,
                        Constants.HeaderNamespace,
                        "abcdef"));

                proxy.Enter(11);
                proxy.Enter(12);
                proxy.Add();
                proxy.Enter(13);
                proxy.Subtract();
                Console.WriteLine();
            }

            ((IClientChannel)proxy).Close();

            Console.WriteLine("Now waiting until the context expires");
            Thread.Sleep(SharedInstanceContextInfo.SecondsToIdle * 1000 + 2);

            Console.WriteLine("Trying again with the original header name...");
            proxy = factory.CreateChannel();
            using (new OperationContextScope((IContextChannel)proxy))
            {
                OperationContext.Current.OutgoingMessageHeaders.Add(
                    MessageHeader.CreateHeader(
                        Constants.HeaderName,
                        Constants.HeaderNamespace,
                        "abcdef"));

                proxy.Enter(14);
                proxy.Enter(15);
                proxy.Add();
                proxy.Enter(16);
                proxy.Subtract();
                Console.WriteLine();
            }

            ((IClientChannel)proxy).Close();

            Console.WriteLine("Now reusing the same instance context for a long time");
            for (int i = 0; i <= 20; i++)
            {
                Thread.Sleep(1000 * SharedInstanceContextInfo.SecondsToIdle / 10);
                proxy = factory.CreateChannel();
                using (new OperationContextScope((IContextChannel)proxy))
                {
                    OperationContext.Current.OutgoingMessageHeaders.Add(
                        MessageHeader.CreateHeader(
                            Constants.HeaderName,
                            Constants.HeaderNamespace,
                            "abcdef"));

                    if (i <= 10)
                    {
                        proxy.Enter(i);
                    }
                    else
                    {
                        proxy.Add();
                    }
                }

                ((IClientChannel)proxy).Close();
            }

            Console.WriteLine("Press ENTER to close");
            Console.ReadLine();

            factory.Close();
            host.Close();
        }
    }
}
