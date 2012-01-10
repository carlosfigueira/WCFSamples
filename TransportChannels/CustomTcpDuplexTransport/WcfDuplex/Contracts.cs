using System.ServiceModel;

namespace WcfDuplex
{
    [ServiceContract(CallbackContract = typeof(ITestCallback))]
    public interface ITest
    {
        [OperationContract]
        void Hello(string text);
    }

    [ServiceContract]
    public interface ITestCallback
    {
        [OperationContract(IsOneWay = true)]
        void OnHello(string text);
    }
}
