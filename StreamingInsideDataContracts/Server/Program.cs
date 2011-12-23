using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Xml.Serialization;

namespace StreamingInsideDataContracts
{
    [XmlRoot(Namespace = "http://my.namespace.com/data")]
    public class RequestClass
    {
        [XmlAttribute]
        public string id;
        [XmlElement]
        public string token;
        [XmlElement]
        public Content content;
    }

    [XmlRoot(Namespace = "http://my.namespace.com/data")]
    public class ResponseClass
    {
        [XmlAttribute]
        public string id;
        [XmlElement]
        public string token;
        [XmlElement]
        public Content content;
    }

    [XmlType(Namespace = "http://my.namespace.com/data")]
    public class Content
    {
        [XmlElement]
        public string name;
        [XmlElement]
        public string extension;
        [XmlElement]
        public byte[] data;
    }

    [ServiceContract(Namespace = "http://my.namespace.com/service")]
    [XmlSerializerFormat]
    public interface ISomeService
    {
        [OperationContract]
        void SendRequest(RequestClass request);
        [OperationContract]
        ResponseClass GetResponse(int dataSize);
    }

    public class SomeServiceImpl : ISomeService
    {
        public void SendRequest(RequestClass request)
        {
            Console.WriteLine(
                "Received request for {0}.{1}, with {2} bytes",
                request.content.name,
                request.content.extension,
                request.content.data.Length);
        }

        public ResponseClass GetResponse(int dataSize)
        {
            byte[] data = new byte[dataSize];
            for (int i = 0; i < dataSize; i++) data[i] = (byte)'b';
            return new ResponseClass
            {
                id = "resp",
                token = "tkn",
                content = new Content
                {
                    name = "resp",
                    extension = "txt",
                    data = data,
                },
            };
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string baseAddress = "http://" + Environment.MachineName + ":8000/Service";
            BasicHttpBinding binding = new BasicHttpBinding();
            ServiceHost host = new ServiceHost(typeof(SomeServiceImpl), new Uri(baseAddress));
            binding.MessageEncoding = WSMessageEncoding.Mtom;
            host.AddServiceEndpoint(typeof(ISomeService), binding, "");
            host.Description.Behaviors.Add(new ServiceMetadataBehavior { HttpGetEnabled = true });
            host.Open();
            Console.WriteLine("Press ENTER to close");
            Console.ReadLine();
            host.Close();
        }
    }
}
