using System;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using CustomTcpDuplex.Channels;
using CustomTcpDuplex.Utils;

namespace CustomTcpDuplex.WcfServer
{
    class Program
    {
        static void Main(string[] args)
        {
            TestWithTypedMessage();
        }

        static void TestWithUntypedMessage()
        {
            string baseAddress = SizedTcpDuplexTransportBindingElement.SizedTcpScheme + "://localhost:8000";
            ServiceHost host = new ServiceHost(typeof(Service), new Uri(baseAddress));
            MessageEncodingBindingElement encodingBE = new BinaryMessageEncodingBindingElement();
            Binding binding = new CustomBinding(encodingBE, new SizedTcpDuplexTransportBindingElement());
            host.AddServiceEndpoint(typeof(IUntypedTest), binding, "");
            host.Open();

            Console.WriteLine("Host opened");

            Socket socket = GetConnectedSocket(8000);

            Message input = Message.CreateMessage(MessageVersion.Soap12WSAddressing10, "myAction", "Hello world");
            input.Headers.To = new Uri(baseAddress);
            input.Headers.ReplyTo = new EndpointAddress("http://www.w3.org/2005/08/addressing/anonymous");

            MessageEncoder encoder = encodingBE.CreateMessageEncoderFactory().Encoder;
            BufferManager bufferManager = BufferManager.CreateBufferManager(int.MaxValue, int.MaxValue);
            ArraySegment<byte> encoded = encoder.WriteMessage(input, int.MaxValue, bufferManager, 4);
            Formatting.SizeToBytes(encoded.Count, encoded.Array, 0);

            Console.WriteLine("Sending those bytes:");
            Debugging.PrintBytes(encoded.Array, encoded.Count + encoded.Offset);
            socket.Send(encoded.Array, 0, encoded.Offset + encoded.Count, SocketFlags.None);
            byte[] recvBuffer = new byte[10000];
            int bytesRecvd = socket.Receive(recvBuffer);
            Console.WriteLine("Received {0} bytes", bytesRecvd);
            Debugging.PrintBytes(recvBuffer, bytesRecvd);

            socket.Close();

            Console.WriteLine("Press ENTER to close");
            Console.ReadLine();
            host.Close();
        }

        static void TestWithTypedMessage()
        {
            string baseAddress = SizedTcpDuplexTransportBindingElement.SizedTcpScheme + "://localhost:8000";
            ServiceHost host = new ServiceHost(typeof(Service), new Uri(baseAddress));
            Binding binding = new CustomBinding(new SizedTcpDuplexTransportBindingElement());
            host.AddServiceEndpoint(typeof(ITypedTest), binding, "");
            host.Open();

            Console.WriteLine("Host opened");

            Socket socket = GetConnectedSocket(8000);

            string request = @"<s:Envelope
        xmlns:s=""http://www.w3.org/2003/05/soap-envelope""
        xmlns:a=""http://www.w3.org/2005/08/addressing"">
    <s:Header>
        <a:Action s:mustUnderstand=""1"">http://tempuri.org/ITypedTest/Add</a:Action>
        <a:MessageID>urn:uuid:c2998797-7312-481a-8f73-230406b12bea</a:MessageID>
        <a:ReplyTo>
            <a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address>
        </a:ReplyTo>
        <a:To s:mustUnderstand=""1"">ENDPOINT_ADDRESS</a:To>
    </s:Header>
    <s:Body>
        <Add xmlns=""http://tempuri.org/"">
            <x>4</x>
            <y>5</y>
        </Add>
    </s:Body>
</s:Envelope>";

            request = request.Replace("ENDPOINT_ADDRESS", baseAddress);

            Encoding encoding = new UTF8Encoding(false);
            int byteCount = encoding.GetByteCount(request);
            byte[] reqBytes = new byte[byteCount + 4];
            Formatting.SizeToBytes(byteCount, reqBytes, 0);
            encoding.GetBytes(request, 0, request.Length, reqBytes, 4);
            Console.WriteLine("Sending those bytes:");
            Debugging.PrintBytes(reqBytes);
            socket.Send(reqBytes);
            byte[] recvBuffer = new byte[10000];
            int bytesRecvd = socket.Receive(recvBuffer);
            Console.WriteLine("Received {0} bytes", bytesRecvd);
            Debugging.PrintBytes(recvBuffer, bytesRecvd);

            Console.WriteLine("Press ENTER to send another request");
            Console.ReadLine();

            request = @"<s:Envelope
        xmlns:s=""http://www.w3.org/2003/05/soap-envelope""
        xmlns:a=""http://www.w3.org/2005/08/addressing"">
    <s:Header>
        <a:Action s:mustUnderstand=""1"">http://tempuri.org/ITypedTest/Subtract</a:Action>
        <a:MessageID>urn:uuid:c2998797-7312-481a-8f73-230406b12bea</a:MessageID>
        <a:ReplyTo>
            <a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address>
        </a:ReplyTo>
        <a:To s:mustUnderstand=""1"">ENDPOINT_ADDRESS</a:To>
    </s:Header>
    <s:Body>
        <Subtract xmlns=""http://tempuri.org/"">
            <x>4</x>
            <y>5</y>
        </Subtract>
    </s:Body>
</s:Envelope>";

            request = request.Replace("ENDPOINT_ADDRESS", baseAddress);
            byteCount = encoding.GetByteCount(request);
            reqBytes = new byte[byteCount + 4];
            Formatting.SizeToBytes(byteCount, reqBytes, 0);
            encoding.GetBytes(request, 0, request.Length, reqBytes, 4);
            Console.WriteLine("Sending those bytes:");
            Debugging.PrintBytes(reqBytes);
            socket.Send(reqBytes);
            bytesRecvd = socket.Receive(recvBuffer);
            Console.WriteLine("Received {0} bytes", bytesRecvd);
            Debugging.PrintBytes(recvBuffer, bytesRecvd);

            socket.Close();

            Console.WriteLine("Press ENTER to close");
            Console.ReadLine();
            host.Close();
        }

        static Socket GetConnectedSocket(int port)
        {
            Socket socket = null;
            IPHostEntry hostEntry = Dns.GetHostEntry(Environment.MachineName);
            for (int i = 0; i < hostEntry.AddressList.Length; i++)
            {
                try
                {
                    IPAddress address = hostEntry.AddressList[i];
                    socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect(new IPEndPoint(address, port));
                    break;
                }
                catch (SocketException)
                {
                }
            }

            return socket;
        }
    }
}
