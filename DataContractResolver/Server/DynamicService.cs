using System;
using Common;

namespace Server
{
    public class DynamicService : IDynamicContract
    {
        public MyHolder EchoHolder(MyHolder holder)
        {
            Console.WriteLine("In service, holder = {0}", holder);
            return holder;
        }

        public MyHolder GetHolder(string typeName, string[] propertyNames, object[] propertyValues)
        {
            Console.WriteLine("In service, creating a type called {0}", typeName);
            Type type = DynamicTypeBuilder.Instance.GetDynamicType(typeName);
            object instance = Activator.CreateInstance(type);
            for (int i = 0; i < propertyNames.Length; i++)
            {
                type.GetProperty(propertyNames[i]).SetValue(instance, propertyValues[i], null);
            }

            return new MyHolder { MyObject = instance };
        }

        public string PutHolder(MyHolder holder)
        {
            Console.WriteLine("In service, holder = {0}", holder);
            return holder.ToString();
        }
    }
}
