using System.ServiceModel;
using System.Threading;

namespace Server
{
    [ServiceContract]
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

    [ServiceBehavior(UseSynchronizationContext = false, IncludeExceptionDetailInFaults = true)]
    public class Service : ICalculator
    {
        public int Add(int x, int y)
        {
            this.UpdateServerFormUi("Add");
            return x + y;
        }

        public int Subtract(int x, int y)
        {
            this.UpdateServerFormUi("Subtract");
            return x - y;
        }

        public int Multiply(int x, int y)
        {
            this.UpdateServerFormUi("Multiply");
            return x * y;
        }

        public int Divide(int x, int y)
        {
            this.UpdateServerFormUi("Divide");
            return x / y;
        }

        private void UpdateServerFormUi(string operationName)
        {
            ServerForm.UserCalled(Thread.CurrentPrincipal.Identity.Name, operationName);
        }
    }
}
