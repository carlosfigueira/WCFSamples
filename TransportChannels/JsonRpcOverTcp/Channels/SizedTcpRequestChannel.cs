using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Channels;
using System.ServiceModel;
using System.Net;
using System.Net.Sockets;

namespace JsonRpcOverTcp.Channels
{
    class SizedTcpRequestChannel : SizedTcpBaseChannel, IRequestChannel
    {
        Uri via;
        EndpointAddress remoteAddress;

        public SizedTcpRequestChannel(MessageEncoder encoder, BufferManager bufferManager, ChannelManagerBase channelManager, EndpointAddress remoteAddress, Uri via)
            : base(encoder, bufferManager, channelManager)
        {
            this.via = via;
            this.remoteAddress = remoteAddress;
        }

        #region IRequestChannel Members

        public IAsyncResult BeginRequest(Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public IAsyncResult BeginRequest(Message message, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public Message EndRequest(IAsyncResult result)
        {
            throw new NotImplementedException();
        }

        public EndpointAddress RemoteAddress
        {
            get { return this.remoteAddress; }
        }

        public Message Request(Message message, TimeSpan timeout)
        {
            base.Send(message, timeout);
            return base.Receive(timeout);
        }

        public Message Request(Message message)
        {
            throw new NotImplementedException();
        }

        public Uri Via
        {
            get { return this.via; }
        }

        #endregion

        protected override void OnOpen(TimeSpan timeout)
        {
            this.Connect();
            base.OnOpen(timeout);
        }

        void Connect()
        {
            Socket socket = null;
            int port = Via.Port;
            if (port == -1)
            {
                port = 8000; // the default port for sized.tcp
            }

            IPHostEntry hostEntry;

            try
            {
                hostEntry = Dns.GetHostEntry(Via.Host);
            }
            catch (SocketException socketException)
            {
                throw new EndpointNotFoundException("Unable to resolve host" + Via.Host, socketException);
            }

            for (int i = 0; i < hostEntry.AddressList.Length; i++)
            {
                try
                {
                    IPAddress address = hostEntry.AddressList[i];
                    socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect(new IPEndPoint(address, port));
                    break;
                }
                catch (SocketException socketException)
                {
                    if (i == hostEntry.AddressList.Length - 1)
                    {
                        throw ConvertSocketException(socketException, "Connect");
                    }
                }
            }

            base.InitializeSocket(socket);
        }
    }
}
