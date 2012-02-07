using System;
using System.ServiceModel;
using System.ServiceModel.Dispatcher;

namespace TrackingClients
{
    class ClientTrackerChannelInitializer : IChannelInitializer
    {
        internal static int ConnectedClientCount = 0;

        public void Initialize(IClientChannel channel)
        {
            ConnectedClientCount++;
            Console.WriteLine("Client {0} initialized", channel.SessionId);
            channel.Closed += ClientDisconnected;
            channel.Faulted += ClientDisconnected;
        }

        static void ClientDisconnected(object sender, EventArgs e)
        {
            Console.WriteLine("Client {0} disconnected", ((IClientChannel)sender).SessionId);
            ConnectedClientCount--;
        }
    }
}
