using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
using Common;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Giving a second for the host to open...");
            Thread.Sleep(1000);

            string endpointAddress = "http://" + Environment.MachineName + ":8000/Service";
            var factory = new ChannelFactory<Server.IDynamicContract>(new BasicHttpBinding(), new EndpointAddress(endpointAddress));
            foreach (var operation in factory.Endpoint.Contract.Operations)
            {
                operation.Behaviors.Find<DataContractSerializerOperationBehavior>().DataContractResolver = new DynamicTypeResolver();
            }

            var proxy = factory.CreateChannel();
            Console.WriteLine("Created the proxy");

            object product = CreateProduct();
            Console.WriteLine("Product: {0}", product);

            Console.WriteLine("Calling the service passing the product...");
            MyHolder input = new MyHolder { MyObject = product };
            MyHolder output = proxy.EchoHolder(input);
            Console.WriteLine("Result: {0}", output);
            Console.WriteLine();

            Console.WriteLine("Now passing a product not known by the server");
            object address = CreateAddress();
            string addressString = proxy.PutHolder(new MyHolder { MyObject = address });
            Console.WriteLine("Result: {0}", addressString);
            Console.WriteLine();

            Type[] dynamicTypes = address.GetType().Assembly.GetTypes();
            Console.WriteLine("Do we know about Person yet?");
            Console.WriteLine("All dynamic types so far");
            foreach (var dynamicType in dynamicTypes)
            {
                Console.WriteLine("  {0}", dynamicType.Name);
            }

            Console.WriteLine("Retrieving a new type from the server");
            output = proxy.GetHolder("Person", new string[] { "Name", "Age" }, new object[] { "John Doe", 33 });
            Console.WriteLine("Output: {0}", output);
            Console.WriteLine("Type of output value: {0}", output.MyObject.GetType());
            Console.WriteLine("Is it on the same assembly as the other types? {0}",
                output.MyObject.GetType().Assembly.Equals(address.GetType().Assembly));
        }

        static object CreateProduct()
        {
            Type productType = DynamicTypeBuilder.Instance.GetDynamicType("Product");
            object product = Activator.CreateInstance(productType);
            productType.GetProperty("Id").SetValue(product, 1, null);
            productType.GetProperty("Name").SetValue(product, "Milk", null);
            productType.GetProperty("Price").SetValue(product, 2.99m, null);
            return product;
        }

        static object CreateAddress()
        {
            Type addressType = DynamicTypeBuilder.Instance.GetDynamicType("Address");
            object address = Activator.CreateInstance(addressType);
            addressType.GetProperty("Street").SetValue(address, "One Microsoft Way", null);
            addressType.GetProperty("City").SetValue(address, "Redmond", null);
            addressType.GetProperty("Zip").SetValue(address, "98052", null);
            return address;
        }
    }
}
