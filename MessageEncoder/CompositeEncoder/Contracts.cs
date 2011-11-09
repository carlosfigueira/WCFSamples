using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Net;

namespace CompositeEncoder
{
    [DataContract]
    public class MyDC
    {
        [DataMember]
        public string Name;
        [DataMember]
        public byte[] Contents;
    }

    [ServiceContract]
    public interface ITest
    {
        [OperationContract]
        MyDC Echo(MyDC input);
    }
 
    public class Service : ITest
    {
        public MyDC Echo(MyDC input)
        {
            HttpRequestMessageProperty requestProperty;
            requestProperty = (HttpRequestMessageProperty)OperationContext.Current.IncomingMessageProperties[HttpRequestMessageProperty.Name];
            Console.WriteLine("In service, incoming content-type = {0}", requestProperty.Headers[HttpRequestHeader.ContentType]);
            return input;
        }
    }
}
