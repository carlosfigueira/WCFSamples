using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Net.Sockets;

namespace JsonRpcOverTcp
{
    class SizedTcpDuplexSessionChannel : SizedTcpBaseChannel, IDuplexSessionChannel
    {
        IDuplexSession session;
        Socket socket = null;
        public SizedTcpDuplexSessionChannel(MessageEncoder encoder, BufferManager bufferManager, ChannelManagerBase channelManager)
            : base(encoder, bufferManager, channelManager)
        {
            this.session = new SizedTcpDuplexSession(this);
        }
        public IAsyncResult BeginWaitForMessage(TimeSpan timeout, AsyncCallback callback, object state)
        {
            throw new NotSupportedException("No peeking support");
        }

        public bool EndWaitForMessage(IAsyncResult result)
        {
            throw new NotSupportedException("No peeking support");
        }

        public EndpointAddress LocalAddress
        {
            get { throw new NotImplementedException(); }
        }

        public Message Receive()
        {
            throw new NotImplementedException();
        }

        public bool WaitForMessage(TimeSpan timeout)
        {
            throw new NotSupportedException("No peeking support");
        }

        public EndpointAddress RemoteAddress
        {
            get { throw new NotImplementedException(); }
        }

        public Uri Via
        {
            get { throw new NotImplementedException(); }
        }

        public IDuplexSession Session
        {
            get { return this.session; }
        }

        class SizedTcpDuplexSession : IDuplexSession
        {
            SizedTcpDuplexSessionChannel channel;
            string id;
            public SizedTcpDuplexSession(SizedTcpDuplexSessionChannel channel)
            {
                this.channel = channel;
                this.id = Guid.NewGuid().ToString();
            }

            public IAsyncResult BeginCloseOutputSession(TimeSpan timeout, AsyncCallback callback, object state)
            {
                throw new NotImplementedException();
            }

            public IAsyncResult BeginCloseOutputSession(AsyncCallback callback, object state)
            {
                throw new NotImplementedException();
            }

            public void CloseOutputSession(TimeSpan timeout)
            {
                if (channel.State != CommunicationState.Closing)
                {
                    channel.ThrowIfDisposedOrNotOpen();
                }

                channel.socket.Shutdown(SocketShutdown.Send);
            }

            public void CloseOutputSession()
            {
                throw new NotImplementedException();
            }

            public void EndCloseOutputSession(IAsyncResult result)
            {
                throw new NotImplementedException();
            }

            public string Id
            {
                get { throw new NotImplementedException(); }
            }
        }
    }
}
