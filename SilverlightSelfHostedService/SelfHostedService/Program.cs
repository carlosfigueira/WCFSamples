using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Text;

namespace SelfHostedService
{
    public class SelfHostedServiceWithSilverlightPolicy
    {
        [ServiceContract]
        public interface ITest
        {
            [OperationContract]
            string Echo(string text);
        }
        [ServiceContract]
        public interface IPolicyRetriever
        {
            [OperationContract, WebGet(UriTemplate = "/clientaccesspolicy.xml")]
            Stream GetSilverlightPolicy();
            [OperationContract, WebGet(UriTemplate = "/crossdomain.xml")]
            Stream GetFlashPolicy();
        }
        public class Service : ITest, IPolicyRetriever
        {
            public string Echo(string text) { return text; }
            Stream StringToStream(string result)
            {
                WebOperationContext.Current.OutgoingResponse.ContentType = "application/xml";
                return new MemoryStream(Encoding.UTF8.GetBytes(result));
            }
            public Stream GetSilverlightPolicy()
            {
                string result = @"<?xml version=""1.0"" encoding=""utf-8""?>
<access-policy>
    <cross-domain-access>
        <policy>
            <allow-from http-request-headers=""*"">
                <domain uri=""*""/>
            </allow-from>
            <grant-to>
                <resource path=""/"" include-subpaths=""true""/>
            </grant-to>
        </policy>
    </cross-domain-access>
</access-policy>";
                return StringToStream(result);
            }
            public Stream GetFlashPolicy()
            {
                string result = @"<?xml version=""1.0""?>
<!DOCTYPE cross-domain-policy SYSTEM ""http://www.macromedia.com/xml/dtds/cross-domain-policy.dtd"">
<cross-domain-policy>
    <allow-access-from domain=""*"" />
</cross-domain-policy>";
                return StringToStream(result);
            }
        }
        public static void Main()
        {
            string baseAddress = "http://" + Environment.MachineName + ":8000";
            ServiceHost host = new ServiceHost(typeof(Service), new Uri(baseAddress));
            host.AddServiceEndpoint(typeof(ITest), new BasicHttpBinding(), "basic");
            host.AddServiceEndpoint(typeof(IPolicyRetriever), new WebHttpBinding(), "").Behaviors.Add(new WebHttpBehavior());
            ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
            smb.HttpGetEnabled = true;
            host.Description.Behaviors.Add(smb);
            host.Open();
            Console.WriteLine("Host opened");

            Console.WriteLine("Press ENTER to close");
            Console.ReadLine();
            host.Close();
        }
    }
}
