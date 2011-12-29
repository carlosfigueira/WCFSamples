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

        protected virtual void InitializeSocket(Socket socket)
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
            int bytesAlreadySent = 0;
            while (bytesAlreadySent < buffer.Count)
            {
                try
                {
                    int bytesSent = socket.Send(buffer.Array, buffer.Offset, buffer.Count, SocketFlags.None);
                    bytesAlreadySent += bytesSent;
                }
                catch (SocketException socketException)
                {
                    throw ConvertSocketException(socketException, "Send");
                }
            }
        }

        IAsyncResult BeginSocketSend(byte[] buffer, AsyncCallback callback, object state)
        {
            return BeginSocketSend(new ArraySegment<byte>(buffer), callback, state);
        }

        IAsyncResult BeginSocketSend(ArraySegment<byte> buffer, AsyncCallback callback, object state)
        {
            return new SocketSendAsyncResult(buffer, this, callback, state);
        }

        void EndSocketSend(IAsyncResult result)
        {
            SocketSendAsyncResult.End(result);
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

        IAsyncResult BeginSocketReceiveBytes(int size, AsyncCallback callback, object state)
        {
            return BeginSocketReceiveBytes(size, true, callback, state);
        }

        IAsyncResult BeginSocketReceiveBytes(int size, bool throwOnEmpty, AsyncCallback callback, object state)
        {
            return new SocketReceiveAsyncResult(size, throwOnEmpty, this, callback, state);
        }

        byte[] EndSocketReceiveBytes(IAsyncResult result)
        {
            return SocketReceiveAsyncResult.End(result);
        }

        IAsyncResult BeginSocketReceive(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
        {
            try
            {
                return socket.BeginReceive(buffer, offset, size, SocketFlags.None, callback, state);
            }
            catch (SocketException socketException)
            {
                throw ConvertSocketException(socketException, "BeginSocketReceive");
            }
        }

        int EndSocketReceive(IAsyncResult result)
        {
            try
            {
                return socket.EndReceive(result);
            }
            catch (SocketException socketException)
            {
                throw ConvertSocketException(socketException, "EndSocketReceive");
            }
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

        public IAsyncResult BeginSendMessage(Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            base.ThrowIfDisposedOrNotOpen();
            ArraySegment<byte> encodedMessage = this.EncodeMessage(message);
            return this.BeginWriteData(encodedMessage, timeout, callback, state);
        }

        public IAsyncResult BeginSendMessage(Message message, AsyncCallback callback, object state)
        {
            return this.BeginSendMessage(message, this.DefaultSendTimeout, callback, state);
        }

        public void EndSendMessage(IAsyncResult result)
        {
            this.EndWriteData(result);
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

        private IAsyncResult BeginWriteData(ArraySegment<byte> data, TimeSpan timeout, AsyncCallback callback, object state)
        {
            ArraySegment<byte> toSend = this.AddLengthToBuffer(data);
            return new SocketSendAsyncResult(toSend, this, callback, state);
        }

        void EndWriteData(IAsyncResult result)
        {
            SocketSendAsyncResult.End(result);
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

        public IAsyncResult BeginReceiveMessage(TimeSpan timeout, AsyncCallback callback, object state)
        {
            base.ThrowIfDisposedOrNotOpen();
            return this.BeginReadData(callback, state);
        }

        public Message EndReceiveMessage(IAsyncResult result)
        {
            ArraySegment<byte> encodedBytes = this.EndReadData(result);
            return this.DecodeMessage(encodedBytes);
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

        IAsyncResult BeginReadData(AsyncCallback callback, object state)
        {
            return new ReadDataAsyncResult(this, callback, state);
        }

        ArraySegment<byte> EndReadData(IAsyncResult result)
        {
            return ReadDataAsyncResult.End(result);
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

        class SocketSendAsyncResult : AsyncResult
        {
            SizedTcpBaseChannel channel;
            ArraySegment<byte> buffer;

            public SocketSendAsyncResult(ArraySegment<byte> buffer, SizedTcpBaseChannel channel, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.channel = channel;
                this.buffer = buffer;

                this.StartSending();
            }

            void StartSending()
            {
                IAsyncResult sendResult = this.channel.socket.BeginSend(this.buffer.Array, this.buffer.Offset, this.buffer.Count, SocketFlags.None, OnSend, this);
                if (!sendResult.CompletedSynchronously)
                {
                    return;
                }

                if (this.CompleteSend(sendResult))
                {
                    base.Complete(true);
                }
                else
                {
                    this.StartSending();
                }
            }

            bool CompleteSend(IAsyncResult result)
            {
                try
                {
                    int bytesSent = channel.socket.EndSend(result);
                    if (bytesSent == this.buffer.Count)
                    {
                        return true;
                    }
                    else
                    {
                        this.buffer = new ArraySegment<byte>(this.buffer.Array, this.buffer.Offset + bytesSent, this.buffer.Count - bytesSent);
                        return false;
                    }
                }
                catch (SocketException socketException)
                {
                    throw ConvertSocketException(socketException, "Send");
                }
            }

            static void OnSend(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                SocketSendAsyncResult thisPtr = (SocketSendAsyncResult)result.AsyncState;
                Exception completionException = null;
                bool shouldComplete = false;
                try
                {
                    if (thisPtr.CompleteSend(result))
                    {
                        shouldComplete = true;
                    }
                    else
                    {
                        thisPtr.StartSending();
                    }
                }
                catch (Exception e)
                {
                    completionException = e;
                }

                if (shouldComplete)
                {
                    thisPtr.Complete(false, completionException);
                }
            }


            public static void End(IAsyncResult result)
            {
                AsyncResult.End<SocketSendAsyncResult>(result);
            }
        }

        class SocketReceiveAsyncResult : AsyncResult
        {
            SizedTcpBaseChannel channel;
            int size;
            int bytesReadTotal;
            byte[] buffer;
            bool throwOnEmpty;

            public SocketReceiveAsyncResult(int size, bool throwOnEmpty, SizedTcpBaseChannel channel, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.size = size;
                this.channel = channel;
                this.throwOnEmpty = throwOnEmpty;
                this.bytesReadTotal = 0;
                this.buffer = channel.bufferManager.TakeBuffer(size);

                bool success = false;
                try
                {
                    IAsyncResult socketReceiveResult = channel.BeginSocketReceive(this.buffer, bytesReadTotal, size, OnReadBytes, this);
                    if (socketReceiveResult.CompletedSynchronously)
                    {
                        if (CompleteReadBytes(socketReceiveResult))
                        {
                            base.Complete(true);
                        }
                    }
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        this.Cleanup();
                    }
                }
            }

            void Cleanup()
            {
                if (this.buffer != null)
                {
                    channel.bufferManager.ReturnBuffer(this.buffer);
                    this.buffer = null;
                }
            }

            bool CompleteReadBytes(IAsyncResult result)
            {
                int bytesRead = channel.EndSocketReceive(result);
                bytesReadTotal += bytesRead;
                if (bytesRead == 0)
                {
                    if (size == 0 || !throwOnEmpty)
                    {
                        channel.bufferManager.ReturnBuffer(this.buffer);
                        this.buffer = null;
                        return true;
                    }
                    else
                    {
                        throw new CommunicationException("Premature EOF reached");
                    }
                }

                while (bytesReadTotal < size)
                {
                    IAsyncResult socketReceiveResult = channel.BeginSocketReceive(buffer, bytesReadTotal, size - bytesReadTotal, OnReadBytes, this);
                    if (!socketReceiveResult.CompletedSynchronously)
                    {
                        return false;
                    }
                }

                return true;
            }

            static void OnReadBytes(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                SocketReceiveAsyncResult thisPtr = (SocketReceiveAsyncResult)result.AsyncState;

                Exception completionException = null;
                bool completeSelf = false;
                try
                {
                    completeSelf = thisPtr.CompleteReadBytes(result);
                }
                catch (Exception e)
                {
                    completeSelf = true;
                    completionException = e;
                    thisPtr.Cleanup();
                }

                if (completeSelf)
                {
                    thisPtr.Complete(false, completionException);
                }
            }

            public static byte[] End(IAsyncResult result)
            {
                try
                {
                    SocketReceiveAsyncResult thisPtr = AsyncResult.End<SocketReceiveAsyncResult>(result);
                    return thisPtr.buffer;
                }
                catch (ObjectDisposedException)
                {
                    return null;
                }
            }
        }

        class ReadDataAsyncResult : AsyncResult
        {
            ArraySegment<byte> buffer;
            SizedTcpBaseChannel channel;
            int dataLength;
            byte[] lengthBytes;
            byte[] data;

            public ReadDataAsyncResult(SizedTcpBaseChannel channel, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.channel = channel;

                bool success = false;
                try
                {
                    IAsyncResult drainLengthResult = channel.BeginSocketReceiveBytes(4, false, OnDrainLength, this);
                    if (drainLengthResult.CompletedSynchronously)
                    {
                        if (CompleteDrainLength(drainLengthResult))
                        {
                            base.Complete(true);
                        }
                    }

                    success = true;
                }
                catch (CommunicationException e)
                {
                    base.Complete(true, e);
                }
                finally
                {
                    if (!success)
                    {
                        this.Cleanup();
                    }
                }
            }

            bool CompleteDrainLength(IAsyncResult result)
            {
                this.lengthBytes = channel.EndSocketReceiveBytes(result);
                if (this.lengthBytes == null)
                {
                    this.buffer = new ArraySegment<byte>();
                    return true;
                }

                this.dataLength = Formatting.BytesToSize(this.lengthBytes, 0);

                IAsyncResult readDataResult = channel.BeginSocketReceiveBytes(this.dataLength, OnReadData, this);
                if (!readDataResult.CompletedSynchronously)
                {
                    return false;
                }

                return CompleteReadData(result);
            }

            bool CompleteReadData(IAsyncResult result)
            {
                data = channel.EndSocketReceiveBytes(result);
                this.buffer = new ArraySegment<byte>(this.data, 0, this.dataLength);
                CleanupLength();
                return true;
            }

            static void OnDrainLength(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                ReadDataAsyncResult thisPtr = (ReadDataAsyncResult)result.AsyncState;

                Exception completionException = null;
                bool completeSelf = false;
                try
                {
                    completeSelf = thisPtr.CompleteDrainLength(result);
                }
                catch (Exception e)
                {
                    completeSelf = true;
                    completionException = e;
                    thisPtr.Cleanup();
                }

                if (completeSelf)
                {
                    thisPtr.Complete(false, completionException);
                }
            }

            static void OnReadData(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                ReadDataAsyncResult thisPtr = (ReadDataAsyncResult)result.AsyncState;

                Exception completionException = null;
                bool completeSelf = false;
                try
                {
                    completeSelf = thisPtr.CompleteReadData(result);
                }
                catch (Exception e)
                {
                    completeSelf = true;
                    completionException = e;
                    thisPtr.Cleanup();
                }

                if (completeSelf)
                {
                    thisPtr.Complete(false, completionException);
                }
            }

            public static ArraySegment<byte> End(IAsyncResult result)
            {
                ReadDataAsyncResult thisPtr = AsyncResult.End<ReadDataAsyncResult>(result);
                return thisPtr.buffer;
            }

            void Cleanup()
            {
                if (this.data != null)
                {
                    this.channel.bufferManager.ReturnBuffer(data);
                    this.data = null;
                }

                CleanupLength();
            }

            void CleanupLength()
            {
                if (this.lengthBytes != null)
                {
                    this.channel.bufferManager.ReturnBuffer(lengthBytes);
                    this.lengthBytes = null;
                }
            }
        }
    }
}
