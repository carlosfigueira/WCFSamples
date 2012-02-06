using System;
using System.IO;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;

namespace GlobAwareService
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseAddres = "http://" + Environment.MachineName + ":8000/Service";
            ServiceHost host = new ServiceHost(typeof(Service), new Uri(baseAddres));
            ServiceEndpoint endpoint = host.AddServiceEndpoint(typeof(ITest), new WebHttpBinding(), "");
            endpoint.Behaviors.Add(new WebHttpBehavior { DefaultOutgoingResponseFormat = WebMessageFormat.Json });
            endpoint.Behaviors.Add(new GlobAwareEndpointBehavior());
            host.Open();
            Console.WriteLine("Host opened");

            string[] allRequests = new string[]
            {
                "name=John+Doe&email=john@doe.com&dateOfBirth=1970-01-01",
                "name=&email=john@doe.com&dateOfBirth=1970-01-01",
                "name=John+Doe&email=john&dateOfBirth=1970-01-01",
                "name=John+Doe&email=john@doe.com&dateOfBirth=1470-01-01",
            };

            foreach (string lang in new string[] { null, "en-US", "es-ES", "pt-BR" })
            {
                if (lang != null)
                {
                    Console.WriteLine("Accept-Language: {0}", lang);
                }

                foreach (string request in allRequests)
                {
                    WebClient c = new WebClient();
                    if (lang != null)
                    {
                        c.Headers[HttpRequestHeader.AcceptLanguage] = lang;
                    }

                    try
                    {
                        Console.WriteLine(c.DownloadString(baseAddres + "/CreatePerson?" + request));
                    }
                    catch (WebException e)
                    {
                        Console.WriteLine(new StreamReader(e.Response.GetResponseStream()).ReadToEnd());
                    }
                }

                Console.WriteLine();
            }
        }
    }
}
