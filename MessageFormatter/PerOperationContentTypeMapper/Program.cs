using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.IO;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Net;

namespace PerOperationContentTypeMapper
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseAddress = "http://" + Environment.MachineName + ":8000/Service";
            ServiceHost host = new ServiceHost(typeof(Service), new Uri(baseAddress));
            WebHttpBinding binding = new WebHttpBinding();
            binding.ContentTypeMapper = new RawContentTypeMapper();
            ServiceEndpoint endpoint = host.AddServiceEndpoint(typeof(Service), binding, "");
            DualModeWebHttpBehavior behavior = new DualModeWebHttpBehavior();
            behavior.DefaultOutgoingResponseFormat = WebMessageFormat.Json;
            endpoint.Behaviors.Add(behavior);
            host.Open();
            Console.WriteLine("Host opened");

            SendRequest(baseAddress + "/Add", "POST", "application/json", "{\"x\":3,\"y\":5}");
            SendRequest(baseAddress + "/Upload", "POST", "application/json", "{\"x\":3,\"y\":5}");
            SendRequest(baseAddress + "/Upload", "POST", "text/plain", "some random text");

            Console.Write("Press ENTER to close the host");
            Console.ReadLine();
            host.Close();
        }
        static string SendRequest(string uri, string method, string contentType, string body)
        {
            string responseBody = null;

            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(uri);
            req.Method = method;
            if (!String.IsNullOrEmpty(contentType))
            {
                req.ContentType = contentType;
            }

            if (body != null)
            {
                byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
                req.GetRequestStream().Write(bodyBytes, 0, bodyBytes.Length);
                req.GetRequestStream().Close();
            }

            HttpWebResponse resp;
            try
            {
                resp = (HttpWebResponse)req.GetResponse();
            }
            catch (WebException e)
            {
                resp = (HttpWebResponse)e.Response;
            }

            if (resp == null)
            {
                responseBody = null;
                Console.WriteLine("Response is null");
            }
            else
            {
                Console.WriteLine("HTTP/{0} {1} {2}", resp.ProtocolVersion, (int)resp.StatusCode, resp.StatusDescription);
                foreach (string headerName in resp.Headers.AllKeys)
                {
                    Console.WriteLine("{0}: {1}", headerName, resp.Headers[headerName]);
                }
                Console.WriteLine();
                Stream respStream = resp.GetResponseStream();
                if (respStream != null)
                {
                    responseBody = new StreamReader(respStream).ReadToEnd();
                    Console.WriteLine(responseBody);
                }
                else
                {
                    Console.WriteLine("HttpWebResponse.GetResponseStream returned null");
                }
            }

            Console.WriteLine();
            Console.WriteLine("  *-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*  ");
            Console.WriteLine();

            return responseBody;
        }
    }
}
