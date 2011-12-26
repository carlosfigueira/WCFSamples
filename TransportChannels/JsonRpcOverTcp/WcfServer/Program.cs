using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;
using JsonRpcOverTcp.Channels;
using System.Net.Sockets;
using System.Net;
using JsonRpcOverTcp.Utils;
using JsonRpcOverTcp.ServiceModel;

namespace WcfServer
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseAddress = "sized.tcp://" + Environment.MachineName + ":8000/";
            ServiceHost host = new ServiceHost(typeof(Service), new Uri(baseAddress));
            CustomBinding binding = new CustomBinding(
                new ByteStreamMessageEncodingBindingElement(),
                new SizedTcpTransportBindingElement());
            //host.AddServiceEndpoint(typeof(IUntypedTest), binding, "");
            host.AddServiceEndpoint(typeof(ITypedTest), binding, "").Behaviors.Add(new JsonRpcEndpointBehavior());
            host.Open();
            Console.WriteLine("Host opened");

            string[] requests = new string[]
            {
                "{\"method\":\"Add\",\"params\":[5, 8],\"id\":1}",
                "{\"method\":\"Multiply\",\"params\":[5, 8],\"id\":2}",
                "{\"method\":\"Divide\",\"params\":[5, 0],\"id\":3}",
            };

            foreach (string request in requests)
            {
                Console.WriteLine("Request: {0}", request);
                Socket socket = GetConnectedSocket(8000);
                byte[] data = Encoding.UTF8.GetBytes(request);
                byte[] toSend = new byte[data.Length + 4];
                Formatting.SizeToBytes(data.Length, toSend, 0);
                Array.Copy(data, 0, toSend, 4, data.Length);
                socket.Send(toSend);
                Console.WriteLine("Sent request to the server");
                byte[] recvBuffer = new byte[1000];
                int bytesReceived = socket.Receive(recvBuffer);
                Console.WriteLine("Received {0} bytes", bytesReceived);
                Debugging.PrintBytes(recvBuffer, bytesReceived);
                socket.Close();
            }
        }

        public static Socket GetConnectedSocket(int port)
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
