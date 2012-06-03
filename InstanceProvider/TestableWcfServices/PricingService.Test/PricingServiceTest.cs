using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PricingService.Test
{
    [TestClass]
    public class PricingServiceTest
    {
        [TestMethod]
        public void EmptyCart()
        {
            PricingService service = new PricingService(new MockProductRepository(x => new Product()));
            Assert.AreEqual(0, service.PriceOrder());
        }

        [TestMethod]
        public void OneItem()
        {
            double price = AnyInstance.AnyDouble;
            PricingService service = new PricingService(new MockProductRepository(x => new Product { Id = x, UnitPrice = price }));
            service.AddToCart(new OrderItem { Amount = 1, ItemId = 1 });
            Assert.AreEqual(price, service.PriceOrder());
        }

        [TestMethod]
        public void MultipleItems()
        {
            double price1 = AnyInstance.AnyDouble;
            double price2 = AnyInstance.AnyDouble + 1;
            int amount1 = AnyInstance.AnyInt;
            int amount2 = AnyInstance.AnyInt + 3;
            PricingService service = new PricingService(new MockProductRepository(x => new Product { Id = x, UnitPrice = x == 1 ? price1 : price2 }));
            service.AddToCart(new OrderItem { ItemId = 1, Amount = amount1 });
            service.AddToCart(new OrderItem { ItemId = 2, Amount = amount2 });
            double expectedPrice = price1 * amount1 + price2 * amount2;
            Assert.AreEqual(expectedPrice, service.PriceOrder());
        }
    }
}
