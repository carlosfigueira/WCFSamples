using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Channels;
using System.Net.Sockets;
using System.ServiceModel;

namespace JsonRpcOverTcp
{
    class SizedTcpBaseChannel : ChannelBase
    {
        const int maxBufferSize = 64 * 1024;

        Socket socket;
        MessageEncoder encoder;
        BufferManager bufferManager;
        object readLock = new object();
        object writeLock = new object();

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
            SocketSend(new ArraySegment<byte>(buffer));
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

        // Address the Message and serialize it into a byte array.
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

        protected virtual Message DecodeMessage(ArraySegment<byte> data)
        {
            if (data.Array == null)
            {
                return null;
            }
            else
            {
                return this.encoder.ReadMessage(data, bufferManager, "application/octet-stream");
            }
        }

        public void Send(Message message, TimeSpan timeout)
        {
            base.ThrowIfDisposedOrNotOpen();
            ArraySegment<byte> encodedBytes = default(ArraySegment<byte>);
            lock (writeLock)
            {
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
        }

        public void Send(Message message)
        {
            this.Send(message, DefaultSendTimeout);
        }

        IAsyncResult BeginWriteData(ArraySegment<byte> data, TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new WriteDataAsyncResult(data, timeout, this, callback, state);
        }

        void EndWriteData(IAsyncResult result)
        {
            WriteDataAsyncResult.End(result);
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
            fullBuffer[0] = (byte)(data.Count >> 24);
            fullBuffer[1] = (byte)(data.Count >> 16);
            fullBuffer[2] = (byte)(data.Count >> 8);
            fullBuffer[3] = (byte)(data.Count);
            Array.Copy(data.Array, data.Offset, fullBuffer, 4, data.Count);
            this.bufferManager.ReturnBuffer(data.Array);
            return new ArraySegment<byte>(fullBuffer, 0, 4 + data.Count);
        }

        public IAsyncResult BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            base.ThrowIfDisposedOrNotOpen();
            return new SendAsyncResult(message, timeout, this, callback, state);
        }

        public IAsyncResult BeginSend(Message message, AsyncCallback callback, object state)
        {
            return BeginSend(message, DefaultSendTimeout, callback, state);
        }

        public void EndSend(IAsyncResult result)
        {
            SendAsyncResult.End(result);
        }

        public Message Receive(TimeSpan timeout)
        {
            base.ThrowIfDisposedOrNotOpen();
            lock (readLock)
            {
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
        }

        ArraySegment<byte> ReadData()
        {
            // 4 bytes lengths

            byte[] preambleBytes = this.SocketReceiveBytes(4, false);
            if (preambleBytes == null)
            {
                return new ArraySegment<byte>();
            }

            int dataLength = (preambleBytes[0] << 24)
                + (preambleBytes[1] << 16)
                + (preambleBytes[2] << 8)
                + preambleBytes[3];

            byte[] data = this.SocketReceiveBytes(dataLength);
            this.bufferManager.ReturnBuffer(preambleBytes);
            return new ArraySegment<byte>(data, 0, dataLength);
        }

        public IAsyncResult BeginReceive(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new ReceiveAsyncResult(timeout, this, callback, state);
        }

        public IAsyncResult BeginReceive(AsyncCallback callback, object state)
        {
            return this.BeginReceive(this.DefaultReceiveTimeout, callback, state);
        }

        public Message EndReceive(IAsyncResult result)
        {
            return ReceiveAsyncResult.End(result);
        }

        public bool TryReceive(TimeSpan timeout, out Message message)
        {
            try
            {
                message = this.Receive(timeout);
                return true;
            }
            catch (TimeoutException)
            {
                message = null;
                return false;
            }
        }

        public IAsyncResult BeginTryReceive(TimeSpan timeout, AsyncCallback callback, object state)
        {
            base.ThrowIfDisposedOrNotOpen();
            return new TryReceiveAsyncResult(timeout, this, callback, state);
        }

        public bool EndTryReceive(IAsyncResult result, out Message message)
        {
            try
            {
                return TryReceiveAsyncResult.End(result, out message);
            }
            catch (TimeoutException)
            {
                message = null;
                return false;
            }
        }

        IAsyncResult BeginReadData(AsyncCallback callback, object state)
        {
            return new ReadDataAsyncResult(this, callback, state);
        }

        ArraySegment<byte> EndReadData(IAsyncResult result)
        {
            return ReadDataAsyncResult.End(result);
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
            static AsyncCallback sendCallback = new AsyncCallback(OnSend);

            public SocketSendAsyncResult(ArraySegment<byte> buffer, SizedTcpBaseChannel channel, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.channel = channel;

                IAsyncResult sendResult = channel.socket.BeginSend(buffer.Array, buffer.Offset, buffer.Count, SocketFlags.None, sendCallback, this);
                if (!sendResult.CompletedSynchronously)
                {
                    return;
                }

                CompleteSend(sendResult);
                base.Complete(true);
            }

            void CompleteSend(IAsyncResult result)
            {
                try
                {
                    channel.socket.EndSend(result);
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
                try
                {
                    thisPtr.CompleteSend(result);
                }
                catch (Exception e)
                {
                    completionException = e;
                }

                thisPtr.Complete(false, completionException);
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

            static AsyncCallback readBytesCallback = new AsyncCallback(OnReadBytes);

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
                    IAsyncResult socketReceiveResult = channel.BeginSocketReceive(this.buffer, bytesReadTotal, size, readBytesCallback, this);
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
                    IAsyncResult socketReceiveResult = channel.BeginSocketReceive(buffer, bytesReadTotal, size - bytesReadTotal, readBytesCallback, this);
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

        class SendAsyncResult : AsyncResult
        {
            SizedTcpBaseChannel channel;
            AsyncCallback writeCallback = new AsyncCallback(OnWrite);

            public SendAsyncResult(Message message, TimeSpan timeout, SizedTcpBaseChannel channel, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.channel = channel;

                ArraySegment<byte> encodedBytes = this.channel.EncodeMessage(message);

                IAsyncResult writeResult = channel.BeginWriteData(encodedBytes, timeout, writeCallback, this);
                if (!writeResult.CompletedSynchronously)
                {
                    return;
                }

                CompleteWrite(writeResult);
                base.Complete(true);
            }

            void CompleteWrite(IAsyncResult result)
            {
                try
                {
                    channel.EndWriteData(result);
                }
                catch (SocketException socketException)
                {
                    throw ConvertSocketException(socketException, "Receive");
                }
            }

            static void OnWrite(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                SendAsyncResult thisPtr = (SendAsyncResult)result.AsyncState;
                Exception completionException = null;
                try
                {
                    thisPtr.CompleteWrite(result);
                }
                catch (Exception e)
                {
                    completionException = e;
                }

                thisPtr.Complete(false, completionException);
            }

            public static void End(IAsyncResult result)
            {
                AsyncResult.End<SendAsyncResult>(result);
            }
        }

        class WriteDataAsyncResult : AsyncResult
        {
            SizedTcpBaseChannel channel;
            ArraySegment<byte> data;
            ArraySegment<byte> toSend;

            public WriteDataAsyncResult(ArraySegment<byte> data, TimeSpan timeout, SizedTcpBaseChannel channel, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.channel = channel;
                this.data = data;
                bool success = false;
                try
                {
                    this.toSend = this.channel.AddLengthToBuffer(data);

                    IAsyncResult sendResult = channel.BeginSocketSend(this.toSend, OnSend, this);
                    if (!sendResult.CompletedSynchronously)
                    {
                        return;
                    }

                    if (CompleteSend(sendResult))
                    {
                        Cleanup();
                        base.Complete(true);
                    }

                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        Cleanup();
                    }
                }
            }

            bool CompleteSend(IAsyncResult asyncResult)
            {
                this.channel.EndSocketSend(asyncResult);
                return true;
            }

            static void OnSend(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                WriteDataAsyncResult thisPtr = (WriteDataAsyncResult)result.AsyncState;
                Exception completionException = null;
                bool completeSelf = false;
                try
                {
                    completeSelf = thisPtr.CompleteSend(result);
                }
                catch (Exception e)
                {
                    completeSelf = true;
                    completionException = e;
                }

                if (completeSelf)
                {
                    thisPtr.Complete(false, completionException);
                }
            }

            public void Cleanup()
            {
                if (this.toSend.Array != null)
                {
                    this.channel.bufferManager.ReturnBuffer(this.toSend.Array);
                    this.toSend = default(ArraySegment<byte>);
                }
            }

            public static void End(IAsyncResult result)
            {
                AsyncResult.End<WriteDataAsyncResult>(result);
            }
        }

        class ReceiveAsyncResult : AsyncResult
        {
            SizedTcpBaseChannel channel;
            Message message;

            public ReceiveAsyncResult(TimeSpan timeout, SizedTcpBaseChannel channel, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.channel = channel;

                if (!channel.IsDisposed)
                {
                    IAsyncResult readDataResult = channel.BeginReadData(OnReceiveData, this);
                    if (!readDataResult.CompletedSynchronously)
                    {
                        return;
                    }

                    CompleteReceiveData(readDataResult);
                }

                base.Complete(true);
            }

            void CompleteReceiveData(IAsyncResult result)
            {
                try
                {
                    ArraySegment<byte> data = channel.EndReadData(result);
                    this.message = channel.DecodeMessage(data);
                }
                catch (CommunicationException e)
                {
                    this.Complete(result.CompletedSynchronously, e);
                }
            }

            static void OnReceiveData(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                ReceiveAsyncResult thisPtr = (ReceiveAsyncResult)result.AsyncState;

                Exception completionException = null;
                try
                {
                    thisPtr.CompleteReceiveData(result);
                }
                catch (Exception e)
                {
                    completionException = e;
                }

                thisPtr.Complete(false, completionException);
            }

            public static Message End(IAsyncResult result)
            {
                ReceiveAsyncResult thisPtr = AsyncResult.End<ReceiveAsyncResult>(result);
                return thisPtr.message;
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

                this.dataLength = (this.lengthBytes[0] << 24)
                    + (this.lengthBytes[1] << 16)
                    + (this.lengthBytes[2] << 8)
                    + this.lengthBytes[3];

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

        class TryReceiveAsyncResult : AsyncResult
        {
            SizedTcpBaseChannel channel;
            static AsyncCallback receiveCallback = new AsyncCallback(OnReceive);
            bool receiveSuccess;
            Message message;

            public TryReceiveAsyncResult(TimeSpan timeout, SizedTcpBaseChannel channel, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.channel = channel;

                bool completeSelf = true;
                if (!channel.IsDisposed)
                {
                    try
                    {
                        IAsyncResult beginReceiveResult = this.channel.BeginReceive(timeout, receiveCallback, this);
                        if (beginReceiveResult.CompletedSynchronously)
                        {
                            CompleteReceive(beginReceiveResult);
                        }
                        else
                        {
                            completeSelf = false;
                        }
                    }
                    catch (TimeoutException)
                    {
                    }
                }

                if (completeSelf)
                {
                    base.Complete(true);
                }
            }

            public static bool End(IAsyncResult result, out Message message)
            {
                TryReceiveAsyncResult thisPtr = AsyncResult.End<TryReceiveAsyncResult>(result);
                message = thisPtr.message;
                return thisPtr.receiveSuccess;
            }

            void CompleteReceive(IAsyncResult result)
            {
                this.message = this.channel.EndReceive(result);
                this.receiveSuccess = true;
            }

            static void OnReceive(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                TryReceiveAsyncResult thisPtr = (TryReceiveAsyncResult)result.AsyncState;
                Exception completionException = null;
                try
                {
                    thisPtr.CompleteReceive(result);
                }
                catch (TimeoutException)
                {
                }
                catch (Exception e)
                {
                    completionException = e;
                }

                thisPtr.Complete(false, completionException);
            }
        }
    }
}
