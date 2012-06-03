using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Json;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace PricingService
{
    class Program
    {
        static void Main(string[] args)
        {
            // Populate the "database"
            string fileName = "Products.json";
            Product[] allProducts = new Product[]
            {
                new Product { Id = 1, Name = "Bread", Unit = "un", UnitPrice = 0.34 },
                new Product { Id = 2, Name = "Milk", Unit = "gal", UnitPrice = 2.99 },
                new Product { Id = 3, Name = "Eggs", Unit = "doz", UnitPrice = 2.25 },
                new Product { Id = 4, Name = "Butter", Unit = "lb", UnitPrice = 3.99 },
                new Product { Id = 5, Name = "Flour", Unit = "lb", UnitPrice = 1.25 },
                new Product { Id = 6, Name = "Sugar", Unit = "lb", UnitPrice = 3.22 },
                new Product { Id = 7, Name = "Vanilla", Unit = "oz", UnitPrice = 5.49 },
            };
            using (FileStream fs = File.Create(fileName))
            {
                new DataContractJsonSerializer(typeof(Product[])).WriteObject(fs, allProducts);
            }

            ProductRepositoryFactory.Instance.SetProductRepository(new FileBasedProductRepository(fileName));

            string baseAddress = "http://" + Environment.MachineName + ":8000/Service";
            ServiceHost host = new ServiceHost(typeof(PricingService), new Uri(baseAddress));
            ServiceEndpoint endpoint = host.AddServiceEndpoint(typeof(IPricingService), new WSHttpBinding(), "");
            host.Open();
            Console.WriteLine("Host opened");

            ChannelFactory<IPricingService> factory = new ChannelFactory<IPricingService>(new WSHttpBinding(), new EndpointAddress(baseAddress));
            IPricingService proxy = factory.CreateChannel();
            proxy.AddToCart(new OrderItem { ItemId = 1, Name = "2 breads", Amount = 2 });
            proxy.AddToCart(new OrderItem { ItemId = 2, Name = "1 galon of milk", Amount = 1 });
            proxy.AddToCart(new OrderItem { ItemId = 3, Name = "1 dozen eggs", Amount = 1 });
            proxy.AddToCart(new OrderItem { ItemId = 4, Name = "2 lbs. butter", Amount = 2 });
            proxy.AddToCart(new OrderItem { ItemId = 5, Name = "1.2 lbs. flour", Amount = 1.2 });
            Console.WriteLine(proxy.PriceOrder());

            Console.WriteLine("Press ENTER to close");
            Console.ReadLine();
            host.Close();
        }
    }
}
