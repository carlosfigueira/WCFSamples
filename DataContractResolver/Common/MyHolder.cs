using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class MyHolder
    {
        [DataMember]
        public object MyObject { get; set; }

        public override string ToString()
        {
            return string.Format("MyHolder[MyObject={0}]", this.MyObject);
        }
    }
}
