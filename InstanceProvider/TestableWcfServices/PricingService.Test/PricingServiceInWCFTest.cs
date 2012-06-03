using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ServiceModel;

namespace PricingService.Test
{
    [TestClass]
    public class PricingServiceInWCFTest
    {
        static readonly string ServiceBaseAddress = "http://" + Environment.MachineName + ":8000/Service";
        static ServiceHost host;
        static Dictionary<int, Product> products;

        IPricingService proxy;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            products = new Dictionary<int, Product>();
            products.Add(1, new Product { Id = 1, UnitPrice = AnyInstance.AnyDouble });
            products.Add(2, new Product { Id = 2, UnitPrice = AnyInstance.AnyDouble + 1.23 });
            ProductRepositoryFactory.Instance.SetProductRepository(
                new MockProductRepository(x => products[x]));

            host = new ServiceHost(typeof(PricingService), new Uri(ServiceBaseAddress));
            host.AddServiceEndpoint(typeof(IPricingService), new WSHttpBinding(), "");
            host.Open();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            host.Close();
        }

        [TestInitialize]
        public void Initialize()
        {
            ChannelFactory<IPricingService> factory = new ChannelFactory<IPricingService>(new WSHttpBinding(), new EndpointAddress(ServiceBaseAddress));
            this.proxy = factory.CreateChannel();
            ((IClientChannel)proxy).Closed += delegate { factory.Close(); };
        }

        [TestCleanup]
        public void Cleanup()
        {
            ((IClientChannel)this.proxy).Close();
        }

        [TestMethod]
        public void EmptyCart()
        {
            Assert.AreEqual(0, proxy.PriceOrder());
        }

        [TestMethod]
        public void OneItem()
        {
            double price = products[1].UnitPrice;
            proxy.AddToCart(new OrderItem { Amount = 1, ItemId = 1 });
            Assert.AreEqual(price, proxy.PriceOrder());
        }

        [TestMethod]
        public void MultipleItems()
        {
            double price1 = products[1].UnitPrice;
            double price2 = products[2].UnitPrice;
            int amount1 = AnyInstance.AnyInt;
            int amount2 = AnyInstance.AnyInt + 3;
            proxy.AddToCart(new OrderItem { ItemId = 1, Amount = amount1 });
            proxy.AddToCart(new OrderItem { ItemId = 2, Amount = amount2 });
            double expectedPrice = price1 * amount1 + price2 * amount2;
            Assert.AreEqual(expectedPrice, proxy.PriceOrder());
        }
    }
}
