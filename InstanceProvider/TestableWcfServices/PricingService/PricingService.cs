using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace PricingService
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.PerSession,
        IncludeExceptionDetailInFaults = true)]
    [InstanceProviderBehavior]
    public class PricingService : IPricingService
    {
        List<OrderItem> cart = new List<OrderItem>();
        IProductRepository productRepository;

        public PricingService(IProductRepository productRepository)
        {
            this.productRepository = productRepository;
        }

        public void AddToCart(OrderItem item)
        {
            this.cart.Add(item);
        }

        public double PriceOrder()
        {
            double result = 0;
            foreach (var item in this.cart)
            {
                Product product = this.productRepository.GetProduct(item.ItemId);
                result += product.UnitPrice * item.Amount;
            }

            this.cart.Clear();
            return result;
        }
    }
}
