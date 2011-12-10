using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using JsonRpcOverTcp.Utils;
using System.Collections;
using System.Net.Sockets;
using System.ServiceModel.Channels;
using System.Threading;

namespace JsonRpcOverTcp.Channels.Test
{
    public class SocketClientTests : IDisposable
    {
        const int Port = 8000;
        SimpleEchoServer server;

        public SocketClientTests()
        {
            this.server = new SimpleEchoServer(Port);
            this.server.StartServing();
        }

        public void Dispose()
        {
            this.server.StopServing();
        }

        [Fact]
        public void SynchronousSendReceive()
        {
            byte[] length = new byte[4];
            byte[] data = new byte[36];
            Random rndGen = new Random();
            rndGen.NextBytes(data);
            Formatting.SizeToBytes(data.Length, length, 0);
            byte[] toSend = new byte[length.Length + data.Length];
            Array.Copy(length, 0, toSend, 0, length.Length);
            Array.Copy(data, 0, toSend, length.Length, data.Length);

            object channel = ReflectionHelper.CreateInstance(
                typeof(SizedTcpTransportBindingElement),
                "JsonRpcOverTcp.Channels.SizedTcpBaseChannel",
                null,
                BufferManager.CreateBufferManager(int.MaxValue, int.MaxValue),
                Mocks.GetChannelManagerBase());
            Socket socket = Mocks.GetConnectedSocket(Port);
            ReflectionHelper.SetField(channel, "socket", socket);

            ReflectionHelper.CallMethod(channel, "SocketSend", toSend);
            ReflectionHelper.CallMethod(channel, "SocketReceiveBytes", toSend.Length);

            Assert.Equal(2, this.server.ReceivedBytes.Count);
            Assert.Equal(length, this.server.ReceivedBytes[0], new ArrayComparer<byte>());
            Assert.Equal(data, this.server.ReceivedBytes[1], new ArrayComparer<byte>());
            socket.Close();
        }

        [Fact]
        public void AsynchronousSendSynchronousReceive()
        {
            byte[] length = new byte[4];
            byte[] data = new byte[36];
            Random rndGen = new Random();
            rndGen.NextBytes(data);
            Formatting.SizeToBytes(data.Length, length, 0);
            byte[] toSend = new byte[length.Length + data.Length];
            Array.Copy(length, 0, toSend, 0, length.Length);
            Array.Copy(data, 0, toSend, length.Length, data.Length);

            ManualResetEvent evt = new ManualResetEvent(false);
            object channel = ReflectionHelper.CreateInstance(
                typeof(SizedTcpTransportBindingElement),
                "JsonRpcOverTcp.Channels.SizedTcpBaseChannel",
                null,
                BufferManager.CreateBufferManager(int.MaxValue, int.MaxValue),
                Mocks.GetChannelManagerBase());
            Socket socket = Mocks.GetConnectedSocket(Port);
            ReflectionHelper.SetField(channel, "socket", socket);

            object state = new object();
            bool success = true;
            ReflectionHelper.CallMethod(channel, "BeginSocketSend", toSend, new AsyncCallback(delegate(IAsyncResult asyncResult)
            {
                try
                {
                    if (!Object.ReferenceEquals(asyncResult.AsyncState, state))
                    {
                        success = false;
                        Console.WriteLine("Error, state not preserved");
                    }
                    else
                    {
                        ReflectionHelper.CallMethod(channel, "EndSocketSend", asyncResult);
                        ReflectionHelper.CallMethod(channel, "SocketReceiveBytes", toSend.Length);

                        try
                        {
                            Assert.Equal(2, this.server.ReceivedBytes.Count);
                            Assert.Equal(length, this.server.ReceivedBytes[0], new ArrayComparer<byte>());
                            Assert.Equal(data, this.server.ReceivedBytes[1], new ArrayComparer<byte>());
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Error: " + e);
                            success = false;
                        }
                    }
                }
                finally
                {
                    evt.Set();
                }
            }), state);

            evt.WaitOne();
            Assert.True(success, "Error in callback");
            socket.Close();
        }

        [Fact]
        public void SynchronousSendAsynchronousReceive()
        {
            byte[] length = new byte[4];
            byte[] data = new byte[36];
            Random rndGen = new Random();
            rndGen.NextBytes(data);
            Formatting.SizeToBytes(data.Length, length, 0);
            byte[] toSend = new byte[length.Length + data.Length];
            Array.Copy(length, 0, toSend, 0, length.Length);
            Array.Copy(data, 0, toSend, length.Length, data.Length);

            object channel = ReflectionHelper.CreateInstance(
                typeof(SizedTcpTransportBindingElement),
                "JsonRpcOverTcp.Channels.SizedTcpBaseChannel",
                null,
                BufferManager.CreateBufferManager(int.MaxValue, int.MaxValue),
                Mocks.GetChannelManagerBase());
            Socket socket = Mocks.GetConnectedSocket(Port);
            ReflectionHelper.SetField(channel, "socket", socket);

            ReflectionHelper.CallMethod(channel, "SocketSend", toSend);
            object state = new object();
            bool success = true;
            ManualResetEvent evt = new ManualResetEvent(false);
            ReflectionHelper.CallMethod(channel, "BeginSocketReceiveBytes", toSend.Length, new AsyncCallback(delegate(IAsyncResult asyncResult)
            {
                try
                {
                    if (!Object.ReferenceEquals(asyncResult.AsyncState, state))
                    {
                        success = false;
                        Console.WriteLine("Error, state not preserved");
                    }
                    else
                    {
                        try
                        {
                            byte[] recvd = (byte[])ReflectionHelper.CallMethod(channel, "EndSocketReceiveBytes", asyncResult);
                            Assert.NotNull(recvd);
                            Assert.True(recvd.Length >= toSend.Length);
                            if (recvd.Length > toSend.Length)
                            {
                                // maybe buffer manager returned a bigger buffer
                                byte[] temp = new byte[toSend.Length];
                                Array.Copy(recvd, 0, temp, 0, temp.Length);
                                recvd = temp;
                            }

                            Assert.Equal(recvd, toSend, new ArrayComparer<byte>());
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Error: " + e);
                            success = false;
                        }
                    }
                }
                finally
                {
                    evt.Set();
                }
            }), state);

            evt.WaitOne();
            Assert.True(success, "Error in callback");
            socket.Close();
        }

        [Fact]
        public void AsynchronousWriteDataSynchronousReadData()
        {
            byte[] length = new byte[4];
            byte[] data = new byte[36];
            BufferManager bufferManager = BufferManager.CreateBufferManager(int.MaxValue, int.MaxValue);
            byte[] dataBuffer = bufferManager.TakeBuffer(data.Length);
            Random rndGen = new Random();
            rndGen.NextBytes(data);
            Array.Copy(data, 0, dataBuffer, 0, data.Length);
            Formatting.SizeToBytes(data.Length, length, 0);

            ManualResetEvent evt = new ManualResetEvent(false);
            object channel = ReflectionHelper.CreateInstance(
                typeof(SizedTcpTransportBindingElement),
                "JsonRpcOverTcp.Channels.SizedTcpBaseChannel",
                null,
                bufferManager,
                Mocks.GetChannelManagerBase());
            Socket socket = Mocks.GetConnectedSocket(Port);
            ReflectionHelper.SetField(channel, "socket", socket);

            object state = new object();
            bool success = true;
            ReflectionHelper.CallMethod(channel, "BeginWriteData", new ArraySegment<byte>(dataBuffer, 0, data.Length), TimeSpan.FromMinutes(1), new AsyncCallback(delegate(IAsyncResult asyncResult)
            {
                try
                {
                    if (!Object.ReferenceEquals(asyncResult.AsyncState, state))
                    {
                        success = false;
                        Console.WriteLine("Error, state not preserved");
                    }
                    else
                    {
                        ReflectionHelper.CallMethod(channel, "EndWriteData", asyncResult);
                        ReflectionHelper.CallMethod(channel, "SocketReceiveBytes", data.Length + length.Length);

                        try
                        {
                            Assert.Equal(2, this.server.ReceivedBytes.Count);
                            Assert.Equal(length, this.server.ReceivedBytes[0], new ArrayComparer<byte>());
                            Assert.Equal(data, this.server.ReceivedBytes[1], new ArrayComparer<byte>());
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Error: " + e);
                            success = false;
                        }
                    }
                }
                finally
                {
                    evt.Set();
                }
            }), state);

            evt.WaitOne();
            Assert.True(success, "Error in callback");
            socket.Close();
        }

        [Fact]
        public void SynchronousWriteDataAsynchronousReadData()
        {
            byte[] length = new byte[4];
            byte[] data = new byte[36];
            Random rndGen = new Random();
            rndGen.NextBytes(data);
            Formatting.SizeToBytes(data.Length, length, 0);
            byte[] toSend = new byte[length.Length + data.Length];
            Array.Copy(length, 0, toSend, 0, length.Length);
            Array.Copy(data, 0, toSend, length.Length, data.Length);

            ManualResetEvent evt = new ManualResetEvent(false);
            object channel = ReflectionHelper.CreateInstance(
                typeof(SizedTcpTransportBindingElement),
                "JsonRpcOverTcp.Channels.SizedTcpBaseChannel",
                null,
                BufferManager.CreateBufferManager(int.MaxValue, int.MaxValue),
                Mocks.GetChannelManagerBase());
            Socket socket = Mocks.GetConnectedSocket(Port);
            ReflectionHelper.SetField(channel, "socket", socket);

            object state = new object();
            bool success = true;
            ReflectionHelper.CallMethod(channel, "SocketSend", toSend);
            ReflectionHelper.CallMethod(channel, "BeginReadData", new AsyncCallback(delegate(IAsyncResult asyncResult)
            {
                try
                {
                    if (!Object.ReferenceEquals(asyncResult.AsyncState, state))
                    {
                        success = false;
                        Console.WriteLine("Error, state not preserved");
                    }
                    else
                    {
                        ArraySegment<byte> result = (ArraySegment<byte>)ReflectionHelper.CallMethod(channel, "EndReadData", asyncResult);

                        try
                        {
                            Assert.Equal(result.Count, data.Length);
                            byte[] temp = new byte[result.Count];
                            Array.Copy(result.Array, result.Offset, temp, 0, result.Count);
                            Assert.Equal(data, temp, new ArrayComparer<byte>());
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Error: " + e);
                            success = false;
                        }
                    }
                }
                finally
                {
                    evt.Set();
                }
            }), state);

            evt.WaitOne();
            Assert.True(success, "Error in callback");
            socket.Close();
        }

        // Next up: AsyncSendMessageSyncReceiveMessage

        class ArrayComparer<T> : IEqualityComparer<T[]>
        {
            public bool Equals(T[] x, T[] y)
            {
                if (x.Length != y.Length) return false;
                for (int i = 0; i < x.Length; i++)
                {
                    if (!Object.Equals(x[i], y[i])) return false;
                }

                return true;
            }

            public int GetHashCode(T[] obj)
            {
                return 0; // unused
            }
        }
    }
}
