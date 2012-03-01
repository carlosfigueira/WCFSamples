using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.ServiceModel.Web;

namespace SLApp.Web
{
    [DataContract]
    public class Person
    {
        [DataMember]
        public int Age;
        [DataMember]
        public string Name;
        [DataMember]
        public DateTime DOB;

        public override string ToString()
        {
            return String.Format("Person[Name={0},Age={1},DOB={2}]", Name, Age, DOB);
        }
    }

    public class Product
    {
        public int ID;
        public string Name;
    }

    public class OrderItem
    {
        public Product Product;
        public double Amount;
    }

    public class Order
    {
        public bool Processed;
        public List<OrderItem> Items;
    }

    [ServiceContract]
    public interface IRestService
    {
        [WebGet(ResponseFormat = WebMessageFormat.Json)]
#if !SILVERLIGHT
        [OperationContract]
        int AddGetJson(int x, int y);
#else
        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginAddGetJson(int x, int y, AsyncCallback callback, object state);
        int EndAddGetJson(IAsyncResult asyncResult);
#endif

        [WebGet]
#if !SILVERLIGHT
        [OperationContract]
        int AddGetXml(int x, int y);
#else
        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginAddGetXml(int x, int y, AsyncCallback callback, object state);
        int EndAddGetXml(IAsyncResult asyncResult);
#endif

        [WebInvoke(RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped)]
#if !SILVERLIGHT
        [OperationContract]
        int SubtractPostJson(int x, int y);
#else
        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginSubtractPostJson(int x, int y, AsyncCallback callback, object state);
        int EndSubtractPostJson(IAsyncResult asyncResult);
#endif

        [WebInvoke(RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml,
            BodyStyle = WebMessageBodyStyle.Wrapped)]
#if !SILVERLIGHT
        [OperationContract]
        int SubtractPostXml(int x, int y);
#else
        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginSubtractPostXml(int x, int y, AsyncCallback callback, object state);
        int EndSubtractPostXml(IAsyncResult asyncResult);
#endif

        [WebGet(ResponseFormat = WebMessageFormat.Json, UriTemplate = "/ReverseGet/{text}")]
#if !SILVERLIGHT
        [OperationContract]
        string ReverseUriTemplateGet(string text);
#else
        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginReverseUriTemplateGet(string text, AsyncCallback callback, object state);
        string EndReverseUriTemplateGet(IAsyncResult asyncResult);
#endif

        [WebInvoke(ResponseFormat = WebMessageFormat.Json, RequestFormat = WebMessageFormat.Json, UriTemplate = "/CreatePerson/{name}", BodyStyle = WebMessageBodyStyle.WrappedRequest)]
#if !SILVERLIGHT
        [OperationContract]
        Person CreatePerson(string name, int age, DateTime dateOfBirth);
#else
        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginCreatePerson(string name, int age, DateTime dateOfBirth, AsyncCallback callback, object state);
        Person EndCreatePerson(IAsyncResult asyncResult);
#endif

        [WebInvoke(ResponseFormat = WebMessageFormat.Json, RequestFormat = WebMessageFormat.Json)]
#if !SILVERLIGHT
        [OperationContract]
        int CreateOrder(Order order);
#else
        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginCreateOrder(Order order, AsyncCallback callback, object state);
        int EndCreateOrder(IAsyncResult asyncResult);
#endif
    }
}
