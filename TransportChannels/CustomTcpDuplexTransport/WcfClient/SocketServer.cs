using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using CustomTcpDuplex.Utils;

namespace CustomTcpDuplex.WcfClient
{
    public class SocketsServer
    {
        Socket listenSocket;
        int port;
        Dispatcher dispatcher;

        public SocketsServer(int port)
        {
            this.listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.port = port;
            this.dispatcher = new Dispatcher();
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
                ReceiveState receiveState = new ReceiveState
                {
                    ClientSocket = clientSocket,
                    Buffer = new byte[4],
                    Offset = 0,
                    Count = 4,
                    ReceivingLength = true,
                };
                clientSocket.BeginReceive(receiveState.Buffer, 0, 4, SocketFlags.None, OnReceive, receiveState);
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
            ReceiveState state = (ReceiveState)asyncResult.AsyncState;
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
                        state.ClientSocket.BeginReceive(state.Buffer, state.Offset + bytesReceived, state.Count - bytesReceived, SocketFlags.None, OnReceive, state);
                    }
                    else
                    {
                        if (state.ReceivingLength)
                        {
                            int length = Formatting.BytesToSize(state.Buffer, state.Offset);
                            Console.WriteLine("Length: {0}", length);
                            state.ReceivingLength = false;
                            state.Buffer = new byte[length];
                            state.Count = length;
                            state.ClientSocket.BeginReceive(state.Buffer, 0, length, SocketFlags.None, OnReceive, state);
                        }
                        else
                        {
                            Debugging.PrintBytes(state.Buffer, state.Count);
                            byte[] lengthBytes = new byte[4];
                            byte[] response = this.dispatcher.DispatchOperation(state.Buffer, state.Offset, state.Count);
                            Formatting.SizeToBytes(response.Length, lengthBytes, 0);
                            state.ClientSocket.Send(lengthBytes);
                            state.ClientSocket.Send(response);

                            ReceiveState receiveState = new ReceiveState
                            {
                                ClientSocket = state.ClientSocket,
                                Buffer = new byte[4],
                                Offset = 0,
                                Count = 4,
                                ReceivingLength = true,
                            };
                            state.ClientSocket.BeginReceive(receiveState.Buffer, 0, 4, SocketFlags.None, OnReceive, receiveState);
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

        class ReceiveState
        {
            public Socket ClientSocket { get; set; }
            public byte[] Buffer { get; set; }
            public int Offset { get; set; }
            public int Count { get; set; }
            public bool ReceivingLength { get; set; }
        }
    }
}
