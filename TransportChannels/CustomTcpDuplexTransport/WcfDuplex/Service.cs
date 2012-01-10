using System;
using System.ServiceModel;
using System.Threading;

namespace WcfDuplex
{
    public class Service : ITest
    {
        public void Hello(string text)
        {
            Console.WriteLine("[server received] {0}", text);
            ITestCallback callback = OperationContext.Current.GetCallbackChannel<ITestCallback>();
            ThreadPool.QueueUserWorkItem(delegate
            {
                for (int i = 1; i <= 5; i++)
                callback.OnHello(text + ", " + i);
            });
        }
    }
}
