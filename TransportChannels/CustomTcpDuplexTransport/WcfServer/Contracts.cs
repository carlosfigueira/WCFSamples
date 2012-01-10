using System.ServiceModel;
using System.ServiceModel.Channels;

namespace CustomTcpDuplex.WcfServer
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
    }
    public class Service : IUntypedTest, ITypedTest
    {
        public Message Process(Message input)
        {
            Message result = Message.CreateMessage(input.Version, "ReplyAction", "The response");
            result.Headers.To = input.Headers.ReplyTo.Uri;
            return result;
        }

        public int Add(int x, int y)
        {
            return x + y;
        }

        public int Subtract(int x, int y)
        {
            return x - y;
        }
    }
}
