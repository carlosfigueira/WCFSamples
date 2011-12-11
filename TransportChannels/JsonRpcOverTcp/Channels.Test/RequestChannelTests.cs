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
using System.ServiceModel;

namespace JsonRpcOverTcp.Channels.Test
{
    public class RequestChannelTests : IDisposable
    {
        const int Port = 8000;
        SimpleEchoServer server;

        public RequestChannelTests()
        {
            this.server = new SimpleEchoServer(Port);
            this.server.StartServing();
        }

        public void Dispose()
        {
            this.server.StopServing();
        }

        [Fact]
        public void AsynchronousSendMessageSyncReceiveMessage()
        {
            byte[] data = new byte[36];
            Random rndGen = new Random();
            rndGen.NextBytes(data);

            byte[] length = new byte[4];
            Formatting.SizeToBytes(data.Length, length, 0);

            Message input = Formatting.BytesToMessage(data);

            ManualResetEvent evt = new ManualResetEvent(false);
            Uri serverUri = new Uri(SizedTcpTransportBindingElement.SizedTcpScheme + "://" + Environment.MachineName + ":" + Port);
            object channel = ReflectionHelper.CreateInstance(
                typeof(SizedTcpTransportBindingElement),
                "JsonRpcOverTcp.Channels.SizedTcpRequestChannel",
                new ByteStreamMessageEncodingBindingElement().CreateMessageEncoderFactory().Encoder,
                BufferManager.CreateBufferManager(int.MaxValue, int.MaxValue),
                Mocks.GetChannelManagerBase(),
                new EndpointAddress(serverUri),
                serverUri);
            ReflectionHelper.CallMethod(channel, "Open");

            object state = new object();
            bool success = true;
            ReflectionHelper.CallMethod(channel, "BeginSendMessage", input, TimeSpan.FromMinutes(1), new AsyncCallback(delegate(IAsyncResult asyncResult)
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
                        ReflectionHelper.CallMethod(channel, "EndSendMessage", asyncResult);
                        Message output = (Message)ReflectionHelper.CallMethod(channel, "ReceiveMessage", TimeSpan.FromMinutes(1));

                        try
                        {
                            Assert.Equal(2, this.server.ReceivedBytes.Count);
                            Assert.Equal(length, this.server.ReceivedBytes[0], new ArrayComparer<byte>());
                            Assert.Equal(data, this.server.ReceivedBytes[1], new ArrayComparer<byte>());

                            byte[] outputBytes = Formatting.MessageToBytes(output);
                            Assert.Equal(data, outputBytes, new ArrayComparer<byte>());
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
            ReflectionHelper.CallMethod(channel, "Close");
        }

        [Fact]
        public void SyncSendMessageAsynchronousReceiveMessage()
        {
            byte[] data = new byte[36];
            Random rndGen = new Random();
            rndGen.NextBytes(data);

            Message input = Formatting.BytesToMessage(data);

            ManualResetEvent evt = new ManualResetEvent(false);
            Uri serverUri = new Uri(SizedTcpTransportBindingElement.SizedTcpScheme + "://" + Environment.MachineName + ":" + Port);
            object channel = ReflectionHelper.CreateInstance(
                typeof(SizedTcpTransportBindingElement),
                "JsonRpcOverTcp.Channels.SizedTcpRequestChannel",
                new ByteStreamMessageEncodingBindingElement().CreateMessageEncoderFactory().Encoder,
                BufferManager.CreateBufferManager(int.MaxValue, int.MaxValue),
                Mocks.GetChannelManagerBase(),
                new EndpointAddress(serverUri),
                serverUri);

            ChannelBase channelBase = (ChannelBase)channel;
            channelBase.Open();

            object state = new object();
            bool success = true;
            ReflectionHelper.CallMethod(channel, "SendMessage", input, TimeSpan.FromMinutes(1));
            ReflectionHelper.CallMethod(channel, "BeginReceiveMessage", TimeSpan.FromMinutes(1), new AsyncCallback(delegate(IAsyncResult asyncResult)
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
                        Message output = (Message)ReflectionHelper.CallMethod(channel, "EndReceiveMessage", asyncResult);

                        try
                        {
                            byte[] outputBytes = Formatting.MessageToBytes(output);
                            Assert.Equal(data, outputBytes, new ArrayComparer<byte>());
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
            channelBase.Close();
        }

        [Fact]
        public void AsynchronousRequest()
        {
            byte[] data = new byte[36];
            Random rndGen = new Random();
            rndGen.NextBytes(data);

            Message input = Formatting.BytesToMessage(data);

            ManualResetEvent evt = new ManualResetEvent(false);
            Uri serverUri = new Uri(SizedTcpTransportBindingElement.SizedTcpScheme + "://" + Environment.MachineName + ":" + Port);
            object channel = ReflectionHelper.CreateInstance(
                typeof(SizedTcpTransportBindingElement),
                "JsonRpcOverTcp.Channels.SizedTcpRequestChannel",
                new ByteStreamMessageEncodingBindingElement().CreateMessageEncoderFactory().Encoder,
                BufferManager.CreateBufferManager(int.MaxValue, int.MaxValue),
                Mocks.GetChannelManagerBase(),
                new EndpointAddress(serverUri),
                serverUri);

            ChannelBase channelBase = (ChannelBase)channel;
            IRequestChannel requestChannel = (IRequestChannel)channel;
            channelBase.Open();

            object state = new object();
            bool success = true;
            requestChannel.BeginRequest(input, new AsyncCallback(delegate(IAsyncResult asyncResult)
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
                        Message output = requestChannel.EndRequest(asyncResult);

                        try
                        {
                            byte[] outputBytes = Formatting.MessageToBytes(output);
                            Assert.Equal(data, outputBytes, new ArrayComparer<byte>());
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
            channelBase.Close();
        }

        [Fact]
        public void AsynchronousOpen()
        {
            ManualResetEvent evt = new ManualResetEvent(false);
            Uri serverUri = new Uri(SizedTcpTransportBindingElement.SizedTcpScheme + "://" + Environment.MachineName + ":" + Port);
            object channel = ReflectionHelper.CreateInstance(
                typeof(SizedTcpTransportBindingElement),
                "JsonRpcOverTcp.Channels.SizedTcpRequestChannel",
                new ByteStreamMessageEncodingBindingElement().CreateMessageEncoderFactory().Encoder,
                BufferManager.CreateBufferManager(int.MaxValue, int.MaxValue),
                Mocks.GetChannelManagerBase(),
                new EndpointAddress(serverUri),
                serverUri);
            object state = new object();
            bool success = true;
            ChannelBase channelBase = (ChannelBase)channel;
            channelBase.BeginOpen(delegate(IAsyncResult asyncResult)
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
                            channelBase.EndOpen(asyncResult);

                            Assert.Equal(CommunicationState.Opened, channelBase.State);
                            Socket socket = (Socket)ReflectionHelper.GetField(channel, "socket");
                            Assert.True(socket.Connected);
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
            }, state);

            evt.WaitOne();
            Assert.True(success, "Error in callback");

            channelBase.Close();
        }

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
