using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace PricingService
{
    [DataContract]
    public class OrderItem
    {
        [DataMember]
        public int ItemId { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public double Amount;
    }

    [DataContract]
    public class Product
    {
        [DataMember]
        public int Id { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Unit { get; set; }
        [DataMember]
        public double UnitPrice { get; set; }
    }
}
