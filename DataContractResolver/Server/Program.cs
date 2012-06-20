using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using Common;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseAddress = "http://" + Environment.MachineName + ":8000/Service";
            ServiceHost host = new ServiceHost(typeof(DynamicService), new Uri(baseAddress));
            ServiceEndpoint endpoint = host.AddServiceEndpoint(typeof(IDynamicContract), new BasicHttpBinding(), "");
            foreach (OperationDescription operation in endpoint.Contract.Operations)
            {
                operation.Behaviors.Find<DataContractSerializerOperationBehavior>().DataContractResolver = new DynamicTypeResolver();
            }

            host.Open();
            Console.WriteLine("Host opened, press ENTER to close");
            Console.ReadLine();
            host.Close();
        }
    }
}
