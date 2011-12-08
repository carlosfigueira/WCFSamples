using System;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.ServiceModel.Channels;

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
            throw new NotImplementedException("Still to be implemented");
        }

        public IAsyncResult BeginRequest(Message message, AsyncCallback callback, object state)
        {
            return this.BeginRequest(message, base.DefaultSendTimeout, callback, state);
        }

        public Message EndRequest(IAsyncResult result)
        {
            throw new NotImplementedException("Still to be implemented");
        }

        public EndpointAddress RemoteAddress
        {
            get { return this.remoteAddress; }
        }

        public Message Request(Message message, TimeSpan timeout)
        {
            base.SendMessage(message, timeout);
            return base.ReceiveMessage(timeout);
        }

        public Message Request(Message message)
        {
            return this.Request(message, base.DefaultSendTimeout);
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
                hostEntry = Dns.GetHostEntry(this.Via.Host);
            }
            catch (SocketException socketException)
            {
                throw new EndpointNotFoundException("Unable to resolve host: " + this.Via.Host, socketException);
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
