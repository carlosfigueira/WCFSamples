using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace SimpleSocketService
{
    class Program
    {
        static void Main(string[] args)
        {
            SimpleServer server = new SimpleServer(8000, new CalculatorService());
            server.StartServing();

            Console.WriteLine("Press ENTER to close");
            Console.ReadLine();

            server.StopServing();
        }
    }
}
