using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace ChangeXmlPrefixes
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseAddress = "http://" + Environment.MachineName + ":8000/Service";
            ServiceHost host = new ServiceHost(typeof(CalculatorService), new Uri(baseAddress));
            Dictionary<string, string> namespaceToPrefixMapping = new Dictionary<string, string>
            {
                { "http://www.w3.org/2003/05/soap-envelope", "SOAP12-ENV" },
                { "http://www.w3.org/2005/08/addressing", "SOAP12-ADDR" },
            };
            Binding binding = ReplacePrefixMessageEncodingBindingElement.ReplaceEncodingBindingElement(
                new WSHttpBinding(SecurityMode.None),
                namespaceToPrefixMapping);
            host.AddServiceEndpoint(typeof(ICalculator), binding, "");
            host.Open();

            Binding clientBinding = LoggingMessageEncodingBindingElement.ReplaceEncodingBindingElement(
                new WSHttpBinding(SecurityMode.None));
            ChannelFactory<ICalculator> factory = new ChannelFactory<ICalculator>(clientBinding, new EndpointAddress(baseAddress));
            ICalculator proxy = factory.CreateChannel();

            Console.WriteLine(proxy.Add(234, 456));

            ((IClientChannel)proxy).Close();
            factory.Close();
            host.Close();
        }
    }
}
