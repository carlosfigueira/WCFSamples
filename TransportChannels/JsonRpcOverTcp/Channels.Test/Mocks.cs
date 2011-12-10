using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Channels;
using System.Net.Sockets;
using System.Net;

namespace JsonRpcOverTcp.Channels.Test
{
    static class Mocks
    {
        public static ChannelManagerBase GetChannelManagerBase()
        {
            HttpTransportBindingElement httpBE = new HttpTransportBindingElement();
            CustomBinding binding = new CustomBinding(httpBE);
            return (ChannelManagerBase)httpBE.BuildChannelFactory<IRequestChannel>(new BindingContext(binding, new BindingParameterCollection()));
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
