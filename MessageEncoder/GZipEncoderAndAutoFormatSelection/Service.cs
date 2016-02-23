using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace GZipEncoderAndAutoFormatSelection
{
    [DataContract(Name = "DataObject", Namespace = "")]
    public class DataObject : IExtensibleDataObject
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public int UID { get; set; }

        public ExtensionDataObject ExtensionData { get; set; }
    }

    [ServiceContract]
    public interface ITest
    {
        [OperationContract]
        [Description("Gets a collection of objects.")]
        [WebGet(UriTemplate = "Objects")]
        List<DataObject> GetObjects();

        [OperationContract]
        [Description("Adds a new Object")]
        [WebInvoke(Method = "POST", UriTemplate = "Objects")]
        void PostObject(DataObject o);
    }

    public class Service : ITest
    {
        protected static List<DataObject> data = new List<DataObject>();

        public List<DataObject> GetObjects()
        {
            return data;
        }

        public void PostObject(DataObject o)
        {
            data.Add(o);
        }
    }
}
