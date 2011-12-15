using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;
using JsonRpcOverTcp.Utils;

namespace WcfServer
{
    [ServiceContract]
    public interface IUntypedTest
    {
        [OperationContract(Action = "*", ReplyAction = "*")]
        Message Process(Message input);
    }

    [ServiceContract]
    public interface ITypedTest
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

    public class Service : ITypedTest, IUntypedTest
    {
        public Message Process(Message input)
        {
            Console.WriteLine("[service] input = {0}", input);
            byte[] bytes = Formatting.MessageToBytes(input);
            Debugging.PrintBytes(bytes);

            return Formatting.BytesToMessage(bytes);
        }

        public int Add(int x, int y)
        {
            return x + y;
        }

        public int Subtract(int x, int y)
        {
            return x - y;
        }

        public int Multiply(int x, int y)
        {
            return x * y;
        }

        public int Divide(int x, int y)
        {
            return x / y;
        }
    }
}
