using System;
using System.Net.Sockets;
using System.ServiceModel;
using System.ServiceModel.Channels;
using JsonRpcOverTcp.Utils;

namespace JsonRpcOverTcp.Channels
{
    class SizedTcpBaseChannel : ChannelBase
    {
        const int maxBufferSize = 64 * 1024;

        Socket socket;
        MessageEncoder encoder;
        BufferManager bufferManager;

        public SizedTcpBaseChannel(MessageEncoder encoder, BufferManager bufferManager, ChannelManagerBase channelManager)
            : base(channelManager)
        {
            this.encoder = encoder;
            this.bufferManager = bufferManager;
        }

        protected void InitializeSocket(Socket socket)
        {
            if (this.socket != null)
            {
                throw new InvalidOperationException("Socket is already set");
            }

            this.socket = socket;
        }

        internal static Exception ConvertSocketException(SocketException socketException, string operation)
        {
            if (socketException.ErrorCode == 10049 // WSAEADDRNOTAVAIL 
                || socketException.ErrorCode == 10061 // WSAECONNREFUSED 
                || socketException.ErrorCode == 10050 // WSAENETDOWN 
                || socketException.ErrorCode == 10051 // WSAENETUNREACH 
                || socketException.ErrorCode == 10064 // WSAEHOSTDOWN 
                || socketException.ErrorCode == 10065) // WSAEHOSTUNREACH
            {
                return new EndpointNotFoundException(string.Format(operation + " error: {0} ({1})", socketException.Message, socketException.ErrorCode), socketException);
            }
            if (socketException.ErrorCode == 10060) // WSAETIMEDOUT
            {
                return new TimeoutException(operation + " timed out.", socketException);
            }
            else
            {
                return new CommunicationException(string.Format(operation + " error: {0} ({1})", socketException.Message, socketException.ErrorCode), socketException);
            }
        }

        void SocketSend(byte[] buffer)
        {
            this.SocketSend(new ArraySegment<byte>(buffer));
        }

        void SocketSend(ArraySegment<byte> buffer)
        {
            try
            {
                socket.Send(buffer.Array, buffer.Offset, buffer.Count, SocketFlags.None);
            }
            catch (SocketException socketException)
            {
                throw ConvertSocketException(socketException, "Send");
            }
        }

        int SocketReceive(byte[] buffer, int offset, int size)
        {
            try
            {
                return socket.Receive(buffer, offset, size, SocketFlags.None);
            }
            catch (SocketException socketException)
            {
                throw ConvertSocketException(socketException, "SocketReceive");
            }
        }

        byte[] SocketReceiveBytes(int size)
        {
            return SocketReceiveBytes(size, true);
        }

        byte[] SocketReceiveBytes(int size, bool throwOnEmpty)
        {
            int bytesReadTotal = 0;
            int bytesRead = 0;
            byte[] data = bufferManager.TakeBuffer(size);

            while (bytesReadTotal < size)
            {
                bytesRead = SocketReceive(data, bytesReadTotal, size - bytesReadTotal);
                bytesReadTotal += bytesRead;
                if (bytesRead == 0)
                {
                    if (bytesReadTotal == 0 && !throwOnEmpty)
                    {
                        bufferManager.ReturnBuffer(data);
                        return null;
                    }
                    else
                    {
                        throw new CommunicationException("Premature EOF reached");
                    }
                }
            }

            return data;
        }

        public void SendMessage(Message message, TimeSpan timeout)
        {
            base.ThrowIfDisposedOrNotOpen();
            ArraySegment<byte> encodedBytes = default(ArraySegment<byte>);
            try
            {
                encodedBytes = this.EncodeMessage(message);
                this.WriteData(encodedBytes);
            }
            catch (SocketException socketException)
            {
                throw ConvertSocketException(socketException, "Send");
            }
            finally
            {
                if (encodedBytes.Array != null)
                {
                    this.bufferManager.ReturnBuffer(encodedBytes.Array);
                }
            }
        }

        ArraySegment<byte> EncodeMessage(Message message)
        {
            try
            {
                return encoder.WriteMessage(message, maxBufferSize, bufferManager);
            }
            finally
            {
                // we've consumed the message by serializing it, so clean up
                message.Close();
            }
        }

        private void WriteData(ArraySegment<byte> data)
        {
            ArraySegment<byte> toSend = this.AddLengthToBuffer(data);
            try
            {
                this.SocketSend(toSend);
            }
            finally
            {
                this.bufferManager.ReturnBuffer(toSend.Array);
            }
        }

        ArraySegment<byte> AddLengthToBuffer(ArraySegment<byte> data)
        {
            byte[] fullBuffer = this.bufferManager.TakeBuffer(data.Count + 4);
            Formatting.SizeToBytes(data.Count, fullBuffer, 0);
            Array.Copy(data.Array, data.Offset, fullBuffer, 4, data.Count);
            this.bufferManager.ReturnBuffer(data.Array);
            return new ArraySegment<byte>(fullBuffer, 0, 4 + data.Count);
        }

        public Message ReceiveMessage(TimeSpan timeout)
        {
            base.ThrowIfDisposedOrNotOpen();
            try
            {
                ArraySegment<byte> encodedBytes = this.ReadData();
                return this.DecodeMessage(encodedBytes);
            }
            catch (SocketException socketException)
            {
                throw ConvertSocketException(socketException, "Receive");
            }
        }

        ArraySegment<byte> ReadData()
        {
            // 4 bytes length
            byte[] preambleBytes = this.SocketReceiveBytes(4, false);
            if (preambleBytes == null)
            {
                return new ArraySegment<byte>();
            }

            int dataLength = Formatting.BytesToSize(preambleBytes, 0);

            byte[] data = this.SocketReceiveBytes(dataLength);
            this.bufferManager.ReturnBuffer(preambleBytes);
            return new ArraySegment<byte>(data, 0, dataLength);
        }

        protected virtual Message DecodeMessage(ArraySegment<byte> data)
        {
            if (data.Array == null)
            {
                return null;
            }
            else
            {
                return this.encoder.ReadMessage(data, bufferManager);
            }
        }

        protected override void OnAbort()
        {
            if (this.socket != null)
            {
                socket.Close(0);
            }
        }

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.OnClose(timeout);
            return new CompletedAsyncResult(callback, state);
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.OnOpen(timeout);
            return new CompletedAsyncResult(callback, state);
        }

        protected override void OnClose(TimeSpan timeout)
        {
            this.socket.Close((int)timeout.TotalMilliseconds);
        }

        protected override void OnEndClose(IAsyncResult result)
        {
            CompletedAsyncResult.End(result);
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            CompletedAsyncResult.End(result);
        }

        protected override void OnOpen(TimeSpan timeout)
        {
        }
    }
}
