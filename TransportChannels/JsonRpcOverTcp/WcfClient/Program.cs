using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using JsonRpcOverTcp.Channels;
using JsonRpcOverTcp.SimpleServer;
using JsonRpcOverTcp.Utils;
using JsonRpcOverTcp.ServiceModel;
using System.Threading;

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

    [ServiceContract]
    public interface ITypedTestAsync
    {
        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginAdd(int x, int y, AsyncCallback callback, object state);
        int EndAdd(IAsyncResult asyncResult);
        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginSubtract(int x, int y, AsyncCallback callback, object state);
        int EndSubtract(IAsyncResult asyncResult);
        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginMultiply(int x, int y, AsyncCallback callback, object state);
        int EndMultiply(IAsyncResult asyncResult);
        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginDivide(int x, int y, AsyncCallback callback, object state);
        int EndDivide(IAsyncResult asyncResult);
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
            catch (JsonRpcException e)
            {
                Console.WriteLine("Error: {0}", e.JsonException);
            }

            Console.WriteLine();
            Console.WriteLine("Now using the typed asynchronous interface");
            var asyncTypedFactory = new ChannelFactory<ITypedTestAsync>(binding, address);
            asyncTypedFactory.Endpoint.Behaviors.Add(new JsonRpcEndpointBehavior());
            ITypedTestAsync asyncTypedProxy = asyncTypedFactory.CreateChannel();

            AutoResetEvent evt = new AutoResetEvent(false);
            Console.WriteLine("Calling BeginAdd");
            asyncTypedProxy.BeginAdd(5, 8, delegate(IAsyncResult ar)
            {
                result = asyncTypedProxy.EndAdd(ar);
                Console.WriteLine("  ==> Result: {0}", result);
                Console.WriteLine();
                evt.Set();
            }, null);
            evt.WaitOne();

            Console.WriteLine("Calling BeginMultiply");
            asyncTypedProxy.BeginMultiply(5, 8, delegate(IAsyncResult ar)
            {
                result = asyncTypedProxy.EndMultiply(ar);
                Console.WriteLine("  ==> Result: {0}", result);
                Console.WriteLine();
                evt.Set();
            }, null);
            evt.WaitOne();

            Console.WriteLine("Calling BeginDivide (throws)");
            asyncTypedProxy.BeginDivide(5, 0, delegate(IAsyncResult ar)
            {
                try
                {
                    result = asyncTypedProxy.EndDivide(ar);
                    Console.WriteLine("  ==> Result: {0}", result);
                }
                catch (JsonRpcException e)
                {
                    Console.WriteLine("Error: {0}", e.JsonException);
                }

                Console.WriteLine();
                evt.Set();
            }, null);
            evt.WaitOne();
        }
    }
}
