using System.ServiceModel;

namespace ChangeXmlPrefixes
{
    [ServiceContract(Name = "ITest", Namespace = "http://service.contract.namespace")]
    public interface ICalculator
    {
        [OperationContract]
        int Add(int x, int y);

        [OperationContract]
        int Subtract(int x, int y);

        [OperationContract]
        int Multiply(int x, int y);

        [OperationContract]
        int Divide(int x, int y);
    }
}
