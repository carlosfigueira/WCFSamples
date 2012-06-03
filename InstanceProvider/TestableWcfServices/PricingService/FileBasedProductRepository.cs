using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace PricingService
{
    public class FileBasedProductRepository : IProductRepository
    {
        private List<Product> products;

        public FileBasedProductRepository(string fileName)
        {
            this.LoadProducts(fileName);
        }

        public Product GetProduct(int productId)
        {
            return this.products.Where(x => x.Id == productId).FirstOrDefault();
        }

        private void LoadProducts(string fileName)
        {
            DataContractJsonSerializer dcs = new DataContractJsonSerializer(typeof(List<Product>));
            using (FileStream fs = File.OpenRead(fileName))
            {
                this.products = (List<Product>)dcs.ReadObject(fs);
            }
        }
    }
}
