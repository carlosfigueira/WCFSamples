using System;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace CompositeEncoder
{
    public class Program
    {
        static Binding GetServerBinding()
        {
            return new CustomBinding(
                new MyReadMtomWriteXmlEncodingBindingElement(),
                new HttpTransportBindingElement());
        }

        public static void Main()
        {
            string baseAddress = "http://" + Environment.MachineName + ":8000/Service";
            ServiceHost host = new ServiceHost(typeof(Service), new Uri(baseAddress));
            host.AddServiceEndpoint(typeof(ITest), GetServerBinding(), "");
            host.Open();
            Console.WriteLine("Host opened");

            foreach (bool useMtom in new bool[] { false, true })
            {
                MessageEncodingBindingElement clientEncoding;
                if (useMtom)
                {
                    clientEncoding = new MtomMessageEncodingBindingElement();
                }
                else
                {
                    clientEncoding = new TextMessageEncodingBindingElement();
                }

                CustomBinding binding = new CustomBinding(clientEncoding, new HttpTransportBindingElement());
                ChannelFactory<ITest> factory = new ChannelFactory<ITest>(binding, new EndpointAddress(baseAddress));
                ITest proxy = factory.CreateChannel();

                byte[] fileContents = new byte[10000];
                for (int i = 0; i < fileContents.Length; i++)
                {
                    fileContents[i] = (byte)('a' + (i % 26));
                }

                using (new OperationContextScope((IContextChannel)proxy))
                {
                    proxy.Echo(new MyDC { Name = "FileName.bin", Contents = fileContents });
                    HttpResponseMessageProperty responseProperty;
                    responseProperty = (HttpResponseMessageProperty)OperationContext.Current.IncomingMessageProperties[HttpResponseMessageProperty.Name];
                    Console.WriteLine("In client, response content-type: {0}", responseProperty.Headers[HttpResponseHeader.ContentType]);
                }

                ((IClientChannel)proxy).Close();
                factory.Close();
            }

            Console.Write("Press ENTER to close the host");
            Console.ReadLine();
            host.Close();
        }
    }
}
