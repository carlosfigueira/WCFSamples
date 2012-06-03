using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace PricingService
{
    public interface IProductRepository
    {
        Product GetProduct(int productId);
    }
}
