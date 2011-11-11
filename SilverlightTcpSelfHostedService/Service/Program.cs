using System;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace Service
{
    [ServiceContract]
    public interface ITest
    {
        [OperationContract]
        string Echo(string text);
    }
    public class Service : ITest
    {
        public string Echo(string text)
        {
            return text;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string baseAddressTcp = "net.tcp://localhost:4504/Service";
            ServiceHost host = new ServiceHost(typeof(Service), new Uri(baseAddressTcp));
            host.AddServiceEndpoint(typeof(ITest), new NetTcpBinding(SecurityMode.None), "");
            ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
            host.Description.Behaviors.Add(smb);
            host.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexTcpBinding(), "mex");
            host.Open();
            Console.WriteLine("Host opened");
            Console.Write("Press ENTER to close");
            Console.ReadLine();
            host.Close();
        }
    }
}
