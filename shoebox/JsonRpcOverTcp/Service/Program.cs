using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.ServiceModel.Description;

namespace Service
{
    [ServiceContract]
    public interface IUntypedTest
    {
        [OperationContract(Action = "*", ReplyAction = "*")]
        Message Process(Message input);
    }

    public class UntypedService : IUntypedTest
    {
        public Message Process(Message input)
        {
            Console.WriteLine("Input: {0}", input);
            byte[] bytes = new byte[20];
            for (int i = 0; i < bytes.Length; i++) bytes[i] = (byte)(i + 'a');
            Message result = Message.CreateMessage(input.Version, null, new BinaryBodyWriter(bytes));
            return result;
        }

        class BinaryBodyWriter : BodyWriter
        {
            byte[] bytes;

            public BinaryBodyWriter(byte[] bytes)
                : base(true)
            {
                this.bytes = bytes;
            }

            protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
            {
                writer.WriteStartElement("Binary");
                writer.WriteBase64(this.bytes, 0, this.bytes.Length);
                writer.WriteEndElement();
            }
        }
    }

    [ServiceContract]
    public interface ITest
    {
        [OperationContract]
        int Add(int x, int y);
        [OperationContract]
        string Echo(string input);
    }

    public class Service : ITest
    {
        public int Add(int x, int y)
        {
            if (x == 0 && y == 0) throw new ArgumentException("Both numbers cannot be zero");
            return x + y;
        }

        public string Echo(string input)
        {
            return input;
        }
    }

    class Program
    {
        static void TestWithUntypedMessage()
        {
            string baseAddress = JsonRpcOverTcp.SizedTcpTransportBindingElement.SizedTcpScheme + "://localhost:8000";
            ServiceHost host = new ServiceHost(typeof(UntypedService), new Uri(baseAddress));
            CustomBinding binding = new CustomBinding(new JsonRpcOverTcp.SizedTcpTransportBindingElement());
            host.AddServiceEndpoint(typeof(IUntypedTest), binding, "");
            host.Open();
            Console.WriteLine("Host opened");

            IPHostEntry entry = Dns.GetHostEntry(Environment.MachineName);
            Socket socket = null;
            for (int i = 0; i < entry.AddressList.Length; i++)
            {
                socket = new Socket(entry.AddressList[i].AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    socket.Connect(entry.AddressList[i], 8000);
                    break;
                }
                catch (SocketException)
                {
                    socket = null;
                }
            }

            if (socket == null)
            {
                throw new InvalidOperationException("Cannot connect socket");
            }

            byte[] input = new byte[] { 0, 0, 0, 10, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57 };
            socket.Send(input);
            byte[] recvBuffer = new byte[1000];
            int bytesRead = socket.Receive(recvBuffer);
            using (FileStream fs = File.Create(@"C:\temp\b.bin"))
            {
                fs.Write(recvBuffer, 0, bytesRead);
            }

            Console.WriteLine("Press ENTER to close");
            Console.ReadLine();
            host.Close();
        }

        static void TestWithTypedMessage()
        {
            string baseAddress = JsonRpcOverTcp.SizedTcpTransportBindingElement.SizedTcpScheme + "://localhost:8000";
            ServiceHost host = new ServiceHost(typeof(Service), new Uri(baseAddress));
            CustomBinding binding = new CustomBinding(new JsonRpcOverTcp.SizedTcpTransportBindingElement());
            ServiceEndpoint endpoint = host.AddServiceEndpoint(typeof(ITest), binding, "");
            endpoint.Behaviors.Add(new JsonRpcOverTcp.JsonRpcEndpointBehavior());
            host.Open();
            Console.WriteLine("Host opened");

            string[] inputs = new string[]
            {
                //"{\"method\":\"Add\", \"params\":[55, 88], \"id\":1}",
                //"{\"method\":\"Add\", \"params\":[55, 99], \"id\":2}",
                //"{\"method\":\"Echo\", \"params\":[\"Hello world\"], \"id\":3}",
                "{\"method\":\"Add\", \"params\":[0,0], \"id\":4, \"comment\":\"this will throw\"}",
            };

            bool foundAddress = false;
            IPAddress serviceAddress = null;
            AddressFamily addressFamily = AddressFamily.Max;
            foreach (var jsonInput in inputs)
            {
                Console.WriteLine("JSON input: {0}", jsonInput);
                Socket socket = null;
                if (!foundAddress)
                {
                    IPHostEntry entry = Dns.GetHostEntry(Environment.MachineName);
                    for (int i = 0; i < entry.AddressList.Length; i++)
                    {
                        socket = new Socket(entry.AddressList[i].AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                        try
                        {
                            socket.Connect(entry.AddressList[i], 8000);
                            serviceAddress = entry.AddressList[i];
                            addressFamily = entry.AddressList[i].AddressFamily;
                            foundAddress = true;
                            break;
                        }
                        catch (SocketException)
                        {
                            socket = null;
                        }
                    }
                }
                else
                {
                    socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        socket.Connect(serviceAddress, 8000);
                    }
                    catch (SocketException)
                    {
                        socket = null;
                    }
                }

                if (socket == null)
                {
                    throw new InvalidOperationException("Cannot connect socket");
                }

                int byteCount = Encoding.UTF8.GetByteCount(jsonInput);
                byte[] input = new byte[byteCount + 4];
                input[0] = (byte)(byteCount >> 24);
                input[1] = (byte)(byteCount >> 16);
                input[2] = (byte)(byteCount >> 8);
                input[3] = (byte)(byteCount);
                Encoding.UTF8.GetBytes(jsonInput, 0, jsonInput.Length, input, 4);
                Console.WriteLine("Input:");
                Util.PrintBytes(input);

                socket.Send(input);
                byte[] recvBuffer = new byte[1000];
                int bytesRead = socket.Receive(recvBuffer);
                Console.WriteLine("Output:");
                Util.PrintBytes(recvBuffer, bytesRead);
                Console.WriteLine("Closing the client socket");
                socket.Close();
                Console.WriteLine("Client socket closed");
            }

            Console.WriteLine("Press ENTER to close");
            Console.ReadLine();
            host.Close();
        }

        static void Main(string[] args)
        {
            TestWithTypedMessage();
        }
    }
}
