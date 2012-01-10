using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;
using CustomTcpDuplex.Channels;

namespace CustomTcpDuplex.WcfClient
{
    [ServiceContract]
    public interface ITypedTest
    {
        [OperationContract]
        int Add(int x, int y);
        [OperationContract]
        int Subtract(int x, int y);
    }

    class Program
    {
        static void Main(string[] args)
        {
            SocketsServer s = new SocketsServer(8000);
            s.StartServing();

            Binding binding = new CustomBinding(new SizedTcpDuplexTransportBindingElement());
            string address = SizedTcpDuplexTransportBindingElement.SizedTcpScheme + "://localhost:8000";
            ChannelFactory<ITypedTest> factory = new ChannelFactory<ITypedTest>(binding, new EndpointAddress(address));
            ITypedTest proxy = factory.CreateChannel();

            Console.WriteLine(proxy.Add(4, 5));
            Console.WriteLine(proxy.Subtract(44, 66));

            s.StopServing();
        }
    }
}
