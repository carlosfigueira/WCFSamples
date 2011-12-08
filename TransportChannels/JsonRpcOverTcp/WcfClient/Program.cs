using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using JsonRpcOverTcp.Utils;
using JsonRpcOverTcp.SimpleServer;
using System.ServiceModel;
using System.ServiceModel.Channels;
using JsonRpcOverTcp.Channels;

namespace JsonRpcOverTcp.WcfClient
{
    [ServiceContract]
    public interface IUntypedTest
    {
        [OperationContract(Action = "*", ReplyAction = "*")]
        Message Process(Message input);

        [OperationContract(Action = "*", ReplyAction = "*", AsyncPattern = true)]
        IAsyncResult BeginProcessAsync(Message input, AsyncCallback callback, object state);
        Message EndProcessAsync(IAsyncResult asyncResult);
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
            EndpointAddress address = new EndpointAddress(SizedTcpTransportBindingElement.SizedTcpScheme + "://localhost:" + port);
            ChannelFactory<IUntypedTest> factory = new ChannelFactory<IUntypedTest>(binding, address);
            IUntypedTest proxy = factory.CreateChannel();

            string largeString = new string('r', 250);
            string[] allInputs = new string[] 
            {
                "{\"method\":\"Add\",\"params\":[5, 8],\"id\":1,\"largeParamIgnored\":\"" + largeString + "\"}",
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
