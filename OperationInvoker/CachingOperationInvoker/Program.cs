using System;
using System.Globalization;
using System.ServiceModel;
using System.Threading;

namespace OperationSelectorExample
{
    class Program
    {
        static void WriteLine(string text, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                text = string.Format(text, args);
            }

            Console.WriteLine("[{0}] {1}", DateTime.Now.ToString("HH:mm:ss.ffffff", CultureInfo.InvariantCulture), text);
        }

        static void Main(string[] args)
        {
            string baseAddress = "http://" + Environment.MachineName + ":8000/Service";
            ServiceHost host = new ServiceHost(typeof(Service), new Uri(baseAddress));
            host.AddServiceEndpoint(typeof(ITest), new BasicHttpBinding(), "");
            host.Open();
            WriteLine("Host opened");

            ChannelFactory<ITest> factory = new ChannelFactory<ITest>(new BasicHttpBinding(), new EndpointAddress(baseAddress));
            ITest proxy = factory.CreateChannel();

            WriteLine("Add(4, 5): {0}", proxy.Add(4, 5));
            WriteLine("Add(4, 5): {0}", proxy.Add(4, 5));

            AutoResetEvent evt = new AutoResetEvent(false);
            proxy.BeginPower(2, 64, delegate(IAsyncResult asyncResult)
            {
                WriteLine("Pow(2, 64): {0}", proxy.EndPower(asyncResult));
                evt.Set();
            }, null);
            evt.WaitOne();

            proxy.BeginPower(2, 64, delegate(IAsyncResult asyncResult)
            {
                WriteLine("Pow(2, 64): {0}", proxy.EndPower(asyncResult));
                evt.Set();
            }, null);
            evt.WaitOne();

            WriteLine("Reverse(\"Hello world\"): {0}", proxy.Reverse("Hello world"));
            WriteLine("Reverse(\"Hello world\"): {0}", proxy.Reverse("Hello world"));

            int i;
            WriteLine("TryParseInt(123): {0}, {1}", proxy.TryParseInt("123", out i), i);
            WriteLine("TryParseInt(123): {0}, {1}", proxy.TryParseInt("123", out i), i);

            proxy.BeginTryParseDouble("34.567", delegate(IAsyncResult asyncResult)
            {
                double dbl;
                WriteLine("TryParseDouble(34.567): {0}, {1}", proxy.EndTryParseDouble(out dbl, asyncResult), dbl);
                evt.Set();
            }, null);
            evt.WaitOne();

            proxy.BeginTryParseDouble("34.567", delegate(IAsyncResult asyncResult)
            {
                double dbl;
                WriteLine("TryParseDouble(34.567): {0}, {1}", proxy.EndTryParseDouble(out dbl, asyncResult), dbl);
                evt.Set();
            }, null);
            evt.WaitOne();

            WriteLine("Press ENTER to close");
            Console.ReadLine();
            host.Close();
        }
    }
}
