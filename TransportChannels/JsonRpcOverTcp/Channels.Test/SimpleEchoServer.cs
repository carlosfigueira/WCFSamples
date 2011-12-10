using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using JsonRpcOverTcp.Utils;

namespace JsonRpcOverTcp.Channels.Test
{
    class SimpleEchoServer
    {
        Socket listenSocket;
        int port;
        List<byte[]> receivedBytes;

        public SimpleEchoServer(int port)
        {
            this.port = port;
            this.listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.receivedBytes = new List<byte[]>();
        }

        public int Port
        {
            get { return this.port; }
        }

        public List<byte[]> ReceivedBytes
        {
            get { return this.receivedBytes; }
        }

        public void ResetReceivedBytes()
        {
            this.receivedBytes.Clear();
        }

        public void StartServing()
        {
            IPEndPoint ipLocal = new IPEndPoint(IPAddress.Any, this.port);
            this.listenSocket.Bind(ipLocal);
            this.listenSocket.Listen(5);
            this.listenSocket.BeginAccept(OnAccept, null);
        }

        public void StopServing()
        {
            this.listenSocket.Close();
        }

        void OnAccept(IAsyncResult asyncResult)
        {
            Socket clientSocket = null;
            try
            {
                clientSocket = listenSocket.EndAccept(asyncResult);

                // Wait for next client
                this.listenSocket.BeginAccept(OnAccept, null);

                IPEndPoint clientIp = (IPEndPoint)clientSocket.RemoteEndPoint;
                Console.WriteLine("Connection from {0}.{1}", clientIp.Address, clientIp.Port);
                SendReceiveState receiveState = new SendReceiveState
                {
                    Server = this,
                    ClientState = SimpleEchoServer.ClientState.ReceivingLength,
                    ClientSocket = clientSocket,
                    Buffer = new byte[4],
                    SendReceiveOffset = 0,
                    Count = 4,
                };

                clientSocket.BeginReceive(receiveState.Buffer, receiveState.SendReceiveOffset, receiveState.Count, SocketFlags.None, OnReceive, receiveState);
            }
            catch (ObjectDisposedException)
            {
                // socket closed
            }
            catch (SocketException e)
            {
                Console.WriteLine("Socket exception: {0}", e);
            }
        }

        void OnReceive(IAsyncResult asyncResult)
        {
            SendReceiveState state = (SendReceiveState)asyncResult.AsyncState;
            try
            {
                int bytesReceived = state.ClientSocket.EndReceive(asyncResult);
                if (bytesReceived == 0)
                {
                    // Client closing the socket
                    state.ClientSocket.Close();
                }
                else
                {
                    if (bytesReceived < state.Count)
                    {
                        state.SendReceiveOffset += bytesReceived;
                        state.ClientSocket.BeginReceive(state.Buffer, state.SendReceiveOffset, state.Count - state.SendReceiveOffset, SocketFlags.None, OnReceive, state);
                    }
                    else
                    {
                        switch (state.ClientState)
                        {
                            case ClientState.ReceivingLength:
                                Console.WriteLine("Received {0} bytes", state.Count);
                                this.AddReceivedBytes(state);
                                int length = Formatting.BytesToSize(state.Buffer, 0);
                                Console.WriteLine("Length: {0}", length);
                                state.ClientState = ClientState.ReceivingData;
                                state.Buffer = new byte[length];
                                state.SendReceiveOffset = 0;
                                state.Count = length;
                                state.ClientSocket.BeginReceive(state.Buffer, 0, length, SocketFlags.None, OnReceive, state);
                                break;
                            case ClientState.ReceivingData:
                                Console.WriteLine("Received {0} bytes", state.Count);
                                this.AddReceivedBytes(state);
                                state.ClientState = ClientState.SendingLength;
                                state.SendReceiveOffset = 0;
                                state.Data = state.Buffer;
                                state.Buffer = new byte[4];
                                Formatting.SizeToBytes(state.Count, state.Buffer, 0);
                                state.Count = 4;
                                state.ClientSocket.BeginSend(state.Buffer, 0, 4, SocketFlags.None, OnSend, state);
                                break;
                        }
                    }
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
        }

        void OnSend(IAsyncResult asyncResult)
        {
            SendReceiveState state = (SendReceiveState)asyncResult.AsyncState;
            try
            {
                int bytesSent = state.ClientSocket.EndSend(asyncResult);
                if (bytesSent == 0)
                {
                    // Client closing the socket
                    state.ClientSocket.Close();
                }
                else
                {
                    if (bytesSent < state.Count)
                    {
                        state.SendReceiveOffset += bytesSent;
                        state.ClientSocket.BeginSend(state.Buffer, state.SendReceiveOffset, state.Count - state.SendReceiveOffset, SocketFlags.None, OnSend, state);
                    }
                    else
                    {
                        switch (state.ClientState)
                        {
                            case ClientState.SendingLength:
                                Console.WriteLine("Sent {0} bytes for the length", state.Count);
                                state.ClientState = ClientState.SendingData;
                                state.Buffer = state.Data;
                                state.SendReceiveOffset = 0;
                                state.Count = state.Data.Length;
                                state.ClientSocket.BeginSend(state.Buffer, 0, state.Count, SocketFlags.None, OnSend, state);
                                break;
                            case ClientState.SendingData:
                                Console.WriteLine("Sent {0} bytes for the data", state.Count);
                                Debugging.PrintBytes(state.Buffer, state.Count);
                                SendReceiveState receiveState = new SendReceiveState
                                {
                                    Server = this,
                                    ClientState = SimpleEchoServer.ClientState.ReceivingLength,
                                    ClientSocket = state.ClientSocket,
                                    Buffer = new byte[4],
                                    SendReceiveOffset = 0,
                                    Count = 4,
                                };
                                Formatting.SizeToBytes(state.Count, state.Buffer, 0);
                                state.ClientSocket.BeginReceive(state.Buffer, 0, 4, SocketFlags.None, OnReceive, receiveState);
                                break;
                        }
                    }
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
        }

        void AddReceivedBytes(SendReceiveState state)
        {
            byte[] received = new byte[state.Count];
            Array.Copy(state.Buffer, received, state.Count);
            this.receivedBytes.Add(received);
        }

        enum ClientState
        {
            ReceivingLength,
            ReceivingData,
            SendingLength,
            SendingData,
        }

        class SendReceiveState
        {
            public SimpleEchoServer Server { get; set; }
            public ClientState ClientState { get; set; }
            public Socket ClientSocket { get; set; }
            public byte[] Buffer { get; set; }
            public byte[] Data { get; set; }
            public int SendReceiveOffset { get; set; }
            public int Count { get; set; }
        }
    }
}
