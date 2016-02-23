using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;

namespace GZipEncoderAndAutoFormatSelection
{
    class Program
    {
        static string BaseAddress = "http://localhost:8000/Service";

        static Binding GetBinding()
        {
            CustomBinding custom = new CustomBinding(new WebHttpBinding());
            for (int i = 0; i < custom.Elements.Count; i++)
            {
                if (custom.Elements[i] is WebMessageEncodingBindingElement)
                {
                    WebMessageEncodingBindingElement webBE = (WebMessageEncodingBindingElement)custom.Elements[i];
                    custom.Elements[i] = new GZipMessageEncodingBindingElement(webBE);
                }
                else if (custom.Elements[i] is TransportBindingElement)
                {
                    ((TransportBindingElement)custom.Elements[i]).MaxReceivedMessageSize = int.MaxValue;
                }
            }

            return custom;
        }

        static void Main(string[] args)
        {
            ServiceHost host = new ServiceHost(typeof(Service), new Uri(BaseAddress));
            ServiceEndpoint ep = host.AddServiceEndpoint(typeof(ITest), GetBinding(), "");
            ep.Behaviors.Add(new WebHttpBehavior { HelpEnabled = true, AutomaticFormatSelectionEnabled = true });
            host.Open();
            Console.WriteLine("Host opened");

            string objectJson1 = "{'Name':'Scooby Doo', 'Description':'A dog', 'UID':123}".Replace('\'', '\"');
            CallService("POST", "application/json", objectJson1, "application/json", true);

            string objectJson2 = "{'Name':'Shaggy', 'Description':'Best friend', 'UID':234}".Replace('\'', '\"');
            CallService("POST", "application/json", objectJson2, "text/xml", true);

            string objectXML1 = @"
        <DataObject>
            <Description>The smart one</Description>
            <Name>Velma</Name>
            <UID>345</UID>
        </DataObject>";
            CallService("POST", "text/xml", objectXML1, "text/xml", true);

            CallService("GET", null, null, "application/json", false);
            CallService("GET", null, null, "text/xml", false);

            Console.Write("Press ENTER to close the host");
            Console.ReadLine();
            host.Close();
        }

        static void CallService(string method, string contentType, string content, string accept, bool compressRequest)
        {
            HttpWebRequest req = HttpWebRequest.CreateHttp(BaseAddress + "/Objects");
            req.Method = method;
            Console.WriteLine("Calling {0} /Objects", method);

            if (contentType != null)
            {
                req.ContentType = contentType;
            }

            if (accept != null)
            {
                req.Accept = accept;
                Console.WriteLine("Sending request with Accept: {0}", req.Accept);
            }

            if (content != null)
            {
                Stream reqStream = req.GetRequestStream();
                if (compressRequest)
                {
                    reqStream = new GZipStream(reqStream, CompressionMode.Compress, false);
                }

                byte[] contentBytes = Encoding.UTF8.GetBytes(content);
                reqStream.Write(contentBytes, 0, contentBytes.Length);
                reqStream.Close();
            }

            HttpWebResponse resp = null;
            try
            {
                resp = (HttpWebResponse)req.GetResponse();
            }
            catch (WebException ex)
            {
                resp = (HttpWebResponse)ex.Response;
            }

            Console.WriteLine("HTTP/{0} {1} {2}", resp.ProtocolVersion, (int)resp.StatusCode, resp.StatusDescription);
            foreach (var header in resp.Headers.AllKeys)
            {
                Console.WriteLine("{0}: {1}", header, resp.Headers[header]);
            }

            var ms = new MemoryStream();
            resp.GetResponseStream().CopyTo(ms);
            string body;
            
            bool gzipEncoding = true;
            if (gzipEncoding)
            {
                using (GZipStream gzip = new GZipStream(ms, CompressionMode.Decompress))
                {
                    ms.Position = 0;
                    var ms2 = new MemoryStream();
                    gzip.CopyTo(ms2);
                    body = Encoding.UTF8.GetString(ms2.ToArray());
                }
            }
            else
            {
                body = Encoding.UTF8.GetString(ms.ToArray());
            }

            Console.WriteLine();
            Console.WriteLine(body);

            Console.WriteLine();
            Console.WriteLine("=-=-=-=-=-=-=-=-=-=-=-=-");
            Console.WriteLine();
        }
    }
}
