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
    public class ReplyChannelTests : IDisposable
    {
        const int Port = 8000;

        public ReplyChannelTests()
        {
        }

        public void Dispose()
        {
        }
    }
}
