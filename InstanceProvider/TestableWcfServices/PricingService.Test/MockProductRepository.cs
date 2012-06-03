using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PricingService.Test
{
    public class MockProductRepository : IProductRepository
    {
        Func<int, Product> getProductFunction;
        public MockProductRepository(Func<int, Product> getProductFunction)
        {
            this.getProductFunction = getProductFunction;
        }

        public Product GetProduct(int productId)
        {
            return this.getProductFunction.Invoke(productId);
        }
    }
}
