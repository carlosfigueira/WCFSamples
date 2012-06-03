using System;
using System.Linq;
using System.ServiceModel;
using System.Threading;

namespace OperationSelectorExample
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class Service : ITest
    {
        public int Add(int x, int y)
        {
            Thread.Sleep(1000);
            return x + y;
        }

        public string Reverse(string input)
        {
            Thread.Sleep(1000);
            return new string(input.Reverse().ToArray());
        }

        public IAsyncResult BeginPower(double x, double y, AsyncCallback callback, object state)
        {
            Func<double, double, double> pow = this.PowerDoWork;
            return pow.BeginInvoke(x, y, callback, state);
        }

        public double EndPower(IAsyncResult asyncResult)
        {
            Func<double, double, double> pow = ((System.Runtime.Remoting.Messaging.AsyncResult)asyncResult).AsyncDelegate as Func<double, double, double>;
            return pow.EndInvoke(asyncResult);
        }

        public bool TryParseInt(string input, out int value)
        {
            Thread.Sleep(1000);
            return int.TryParse(input, out value);
        }

        delegate bool TryParseDoubleDelegate(string input, out double value);

        public IAsyncResult BeginTryParseDouble(string input, AsyncCallback callback, object state)
        {
            TryParseDoubleDelegate del = this.TryParseDoubleDoWork;
            double dummy;
            return del.BeginInvoke(input, out dummy, callback, state);
        }

        public bool EndTryParseDouble(out double value, IAsyncResult asyncResult)
        {
            TryParseDoubleDelegate del = ((System.Runtime.Remoting.Messaging.AsyncResult)asyncResult).AsyncDelegate as TryParseDoubleDelegate;
            return del.EndInvoke(out value, asyncResult);
        }

        private double PowerDoWork(double x, double y)
        {
            Thread.Sleep(1000);
            return Math.Pow(x, y);
        }

        private bool TryParseDoubleDoWork(string s, out double value)
        {
            Thread.Sleep(1000);
            return double.TryParse(s, out value);
        }
    }
}
