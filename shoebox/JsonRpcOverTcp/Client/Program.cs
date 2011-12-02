using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;
using JsonRpcOverTcp;
using System.Xml;
using SimpleSocketService;
using System.Threading;

namespace Client
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
            SimpleServer server = new SimpleServer(port, new SimpleSocketService.CalculatorService());
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
                Message inputMessage = Message.CreateMessage(MessageVersion.None, null, new RawBodyWriter(inputBytes));
                Message outputMessage = proxy.Process(inputMessage);
                Console.WriteLine("Received output: {0}", outputMessage);
                XmlDictionaryReader bodyReader = outputMessage.GetReaderAtBodyContents();
                bodyReader.MoveToContent();
                if (bodyReader.NodeType == XmlNodeType.Element && bodyReader.Name == "Binary")
                {
                    bodyReader.Read();
                    byte[] outputBytes = bodyReader.ReadContentAsBase64();
                    Console.WriteLine("Output bytes:");
                    Util.PrintBytes(outputBytes);
                }
                else
                {
                    Console.WriteLine("Unexpected result.");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Now calling with async pattern");
            Console.WriteLine();

            AutoResetEvent evt = new AutoResetEvent(false);

            foreach (string input in allInputs)
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                Console.WriteLine("Input: {0}", input);
                Message inputMessage = Message.CreateMessage(MessageVersion.None, null, new RawBodyWriter(inputBytes));
                proxy.BeginProcessAsync(inputMessage, delegate(IAsyncResult ar)
                {
                    Message outputMessage = proxy.EndProcessAsync(ar);
                    Console.WriteLine("Received output: {0}", outputMessage);
                    XmlDictionaryReader bodyReader = outputMessage.GetReaderAtBodyContents();
                    bodyReader.MoveToContent();
                    if (bodyReader.NodeType == XmlNodeType.Element && bodyReader.Name == "Binary")
                    {
                        bodyReader.Read();
                        byte[] outputBytes = bodyReader.ReadContentAsBase64();
                        Console.WriteLine("Output bytes:");
                        Util.PrintBytes(outputBytes);
                    }
                    else
                    {
                        Console.WriteLine("Unexpected result.");
                    }

                    evt.Set();
                }, null);
                evt.WaitOne();
            }

            Console.WriteLine("Press ENTER to close");
            Console.ReadLine();
            server.StopServing();
        }

        class RawBodyWriter : BodyWriter
        {
            byte[] bytes;
            public RawBodyWriter(byte[] bytes)
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
}
