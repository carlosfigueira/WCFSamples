using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using JsonRpcOverTcp.Channels;
using JsonRpcOverTcp.SimpleServer;
using JsonRpcOverTcp.Utils;
using JsonRpcOverTcp.ServiceModel;

namespace JsonRpcOverTcp.WcfClient
{
    [ServiceContract]
    public interface IUntypedTest
    {
        [OperationContract(Action = "*", ReplyAction = "*")]
        Message Process(Message input);
    }

    [ServiceContract]
    public interface ITypedTest
    {
        [OperationContract]
        int Add(int x, int y);
        [OperationContract]
        int Subtract(int x, int y);
        [OperationContract]
        int Multiply(int x, int y);
        [OperationContract]
        int Divide(int x, int y);
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

            ((IClientChannel)proxy).Close();
            factory.Close();

            Console.WriteLine();
            Console.WriteLine("Now using the typed interface");
            ChannelFactory<ITypedTest> typedFactory = new ChannelFactory<ITypedTest>(binding, address);
            typedFactory.Endpoint.Behaviors.Add(new JsonRpcEndpointBehavior());
            ITypedTest typedProxy = typedFactory.CreateChannel();

            Console.WriteLine("Calling Add");
            int result = typedProxy.Add(5, 8);
            Console.WriteLine("  ==> Result: {0}", result);
            Console.WriteLine();

            Console.WriteLine("Calling Multiply");
            result = typedProxy.Multiply(5, 8);
            Console.WriteLine("  ==> Result: {0}", result);
            Console.WriteLine();

            Console.WriteLine("Calling Divide (throws)");
            try
            {
                result = typedProxy.Divide(5, 0);
                Console.WriteLine("  ==> Result: {0}", result);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e);
            }
        }
    }
}
