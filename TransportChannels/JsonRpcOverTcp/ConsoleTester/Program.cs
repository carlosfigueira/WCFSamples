using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JsonRpcOverTcp.Channels.Test;

namespace JsonRpcOverTcp.ConsoleTester
{
    class Program
    {
        static void Main(string[] args)
        {
            var tests = new RequestChannelTests();
            tests.AsynchronousOpen();
        }
    }
}
