using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace PricingService
{
    public class ProductRepositoryFactory
    {
        private static ProductRepositoryFactory instance;
        private IProductRepository repository;

        private ProductRepositoryFactory() { }

        public static ProductRepositoryFactory Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ProductRepositoryFactory();
                }

                return instance;
            }
        }

        public void SetProductRepository(IProductRepository repository)
        {
            this.repository = repository;
        }

        public IProductRepository CreateProductRepository()
        {
            return this.repository;
        }
    }
}
