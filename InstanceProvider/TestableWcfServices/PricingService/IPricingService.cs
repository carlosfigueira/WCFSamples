using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace PricingService
{
    [ServiceContract(SessionMode = SessionMode.Required)]
    public interface IPricingService
    {
        [OperationContract(IsTerminating = true)]
        double PriceOrder();
        [OperationContract(IsInitiating = true)]
        void AddToCart(OrderItem item);
    }
}
