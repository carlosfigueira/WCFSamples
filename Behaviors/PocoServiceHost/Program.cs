using System;
using System.ServiceModel;

namespace PocoServiceHost
{
    public class Point
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
    
    public class Service
    {
        public int Add(int x, int y) { return x + y; }
        public double Distance(Point p1, Point p2)
        {
            double deltaX = p1.X - p2.X;
            double deltaY = p1.Y - p2.Y;
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }
    }

    public class BadService1_NoDefaultCtor
    {
        public BadService1_NoDefaultCtor(int i) { }
    }

    public class BadService2_RefParameter
    {
        public bool TryParse(string str, out bool result)
        {
            return bool.TryParse(str, out result);
        }
    }

    [ServiceContract(Name = "Service")]
    public interface ITest
    {
        [OperationContract]
        int Add(int x, int y);
        [OperationContract]
        string Echo(string text);
        [OperationContract]
        double Distance(Point p1, Point p2);
    }

    class Program
    {
        static void Main(string[] args)
        {
            string baseAddress = "http://" + Environment.MachineName + ":8000/Service";

            foreach (Type badServiceType in new Type[] { typeof(BadService1_NoDefaultCtor), typeof(BadService2_RefParameter) })
            {
                try
                {
                    new PocoServiceHost(badServiceType, new Uri(baseAddress)).Open();
                    Console.WriteLine("This line should not be reached");
                }
                catch (InvalidOperationException e)
                {
                    Console.WriteLine("Caught expected exception for service {0}: {1}", badServiceType.Name, e.Message);
                }
            }

            PocoServiceHost host = new PocoServiceHost(typeof(Service), new Uri(baseAddress));
            host.Open();

            ChannelFactory<ITest> factory = new ChannelFactory<ITest>(new BasicHttpBinding(), new EndpointAddress(baseAddress));
            ITest proxy = factory.CreateChannel();

            Console.WriteLine(proxy.Add(4, 5));
            Console.WriteLine(proxy.Distance(new Point { X = 0, Y = 0 }, new Point { X = 3, Y = 4 }));

            Console.WriteLine("Press ENTER to close");
            Console.ReadLine();
            host.Close();
        }
    }
}
