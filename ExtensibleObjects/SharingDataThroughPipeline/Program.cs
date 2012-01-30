using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace SharingDataThroughPipeline
{
    class Program
    {
        static readonly string baseAddressHttp = "http://" + Environment.MachineName + ":8000/Service";
        static readonly string baseAddressPipe = "net.pipe://localhost/Service";
        
        static ICalculator CreateProxy(bool useHttp, bool addErrors = false)
        {
            Binding binding = useHttp ?
                (Binding)new BasicHttpBinding() :
                (Binding)new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
            string address = useHttp ? baseAddressHttp : baseAddressPipe;
            ChannelFactory<ICalculator> factory = new ChannelFactory<ICalculator>(binding, new EndpointAddress(address));
            factory.Endpoint.Behaviors.Add(new MyInspector());
            ICalculator result = factory.CreateChannel();
            ((IClientChannel)result).Closed += delegate { factory.Close(); };
            ((IClientChannel)result).Extensions.Add(new MyChannelExtension { Binding = binding, IntroduceErrors = addErrors });

            return result;
        }

        static void Main(string[] args)
        {
            ServiceHost host = new ServiceHost(typeof(CalculatorService), new Uri(baseAddressHttp), new Uri(baseAddressPipe));
            host.AddServiceEndpoint(typeof(ICalculator), new BasicHttpBinding(), "");
            host.AddServiceEndpoint(typeof(ICalculator), new NetNamedPipeBinding(NetNamedPipeSecurityMode.None), "");
            host.Open();
            Console.WriteLine("Host opened");

            ICalculator httpProxy1 = CreateProxy(useHttp: true, addErrors: false);
            DoCalculations(httpProxy1);

            ICalculator httpProxy2 = CreateProxy(useHttp: true, addErrors: true);
            DoCalculations(httpProxy2);

            ICalculator netPipeProxy1 = CreateProxy(useHttp: false, addErrors: false);
            DoCalculations(netPipeProxy1);

            ((IClientChannel)httpProxy1).Close();
            ((IClientChannel)httpProxy2).Close();
            ((IClientChannel)netPipeProxy1).Close();

            host.Close();
        }

        static void DoCalculations(ICalculator proxy)
        {
            MyChannelExtension extension = ((IContextChannel)proxy).Extensions.Find<MyChannelExtension>();
            if (extension != null)
            {
                if (extension.Binding != null)
                {
                    Console.WriteLine("Sending requests over {0}", extension.Binding.Scheme);
                }

                if (extension.IntroduceErrors)
                {
                    Console.WriteLine("Errors will be introduced in the request");
                }
            }

            // Call the Add service operation.
            double value1 = 100.00D;
            double value2 = 15.99D;
            double result = proxy.Add(value1, value2);
            Console.WriteLine("Add({0},{1}) = {2}", value1, value2, result);

            // Call the Subtract service operation.
            value1 = 145.00D;
            value2 = 76.54D;
            result = proxy.Subtract(value1, value2);
            Console.WriteLine("Subtract({0},{1}) = {2}", value1, value2, result);

            // Call the Multiply service operation.
            value1 = 9.00D;
            value2 = 81.25D;
            result = proxy.Multiply(value1, value2);
            Console.WriteLine("Multiply({0},{1}) = {2}", value1, value2, result);

            // Call the Divide service operation.
            value1 = 22.00D;
            value2 = 7.00D;
            result = proxy.Divide(value1, value2);
            Console.WriteLine("Divide({0},{1}) = {2}", value1, value2, result);

            Console.WriteLine();
        }
    }
}
