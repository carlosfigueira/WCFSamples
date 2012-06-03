using System;
using System.ServiceModel;

namespace OperationSelectorExample
{
    [ServiceContract]
    public interface ITest
    {
        [OperationContract]
        int Add(int x, int y);

        [OperationContract]
        [CacheableOperation(SecondsToCache = 10)]
        string Reverse(string input);
        
        [OperationContract(AsyncPattern=true)]
        [CacheableOperation(SecondsToCache = 30)]
        IAsyncResult BeginPower(double x, double y, AsyncCallback callback, object state);
        double EndPower(IAsyncResult asyncResult);

        [OperationContract, CacheableOperation]
        bool TryParseInt(string input, out int value);

        [OperationContract(AsyncPattern = true), CacheableOperation]
        IAsyncResult BeginTryParseDouble(string input, AsyncCallback callback, object state);
        bool EndTryParseDouble(out double value, IAsyncResult asyncResult);
    }
}
