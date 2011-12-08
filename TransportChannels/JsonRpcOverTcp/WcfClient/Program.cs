using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using JsonRpcOverTcp.Channels;
using JsonRpcOverTcp.SimpleServer;
using JsonRpcOverTcp.Utils;

namespace JsonRpcOverTcp.WcfClient
{
    [ServiceContract]
    public interface IUntypedTest
    {
        [OperationContract(Action = "*", ReplyAction = "*")]
        Message Process(Message input);
    }

    class Program
    {
        static void Main(string[] args)
        {
            int port = 8000;
            SocketsServer server = new SocketsServer(port, new CalculatorService());
            server.StartServing();
            Console.WriteLine("Started the simple server");

            CustomBinding binding = new CustomBinding(new SizedTcpTransportBindingElement());
            EndpointAddress address = new EndpointAddress(
                SizedTcpTransportBindingElement.SizedTcpScheme + "://localhost:" + port);
            ChannelFactory<IUntypedTest> factory = new ChannelFactory<IUntypedTest>(binding, address);
            IUntypedTest proxy = factory.CreateChannel();

            string[] allInputs = new string[] 
            {
                "{\"method\":\"Add\",\"params\":[5, 8],\"id\":1}",
                "{\"method\":\"Multiply\",\"params\":[5, 8],\"id\":2}",
                "{\"method\":\"Divide\",\"params\":[5, 0],\"id\":3}",
            };

            foreach (string input in allInputs)
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                Console.WriteLine("Input: {0}", input);
                Message inputMessage = Formatting.BytesToMessage(inputBytes);
                Message outputMessage = proxy.Process(inputMessage);
                Console.WriteLine("Received output: {0}", outputMessage);
                byte[] outputBytes = Formatting.MessageToBytes(outputMessage);
                Console.WriteLine("Output bytes:");
                Debugging.PrintBytes(outputBytes);
            }
        }
    }
}
