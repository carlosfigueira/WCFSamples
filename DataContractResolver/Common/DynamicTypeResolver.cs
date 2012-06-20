using System;
using System.Runtime.Serialization;
using System.Xml;

namespace Common
{
    public class DynamicTypeResolver : DataContractResolver
    {
        const string ResolverNamespace = "http://dynamic/" + DynamicTypeBuilder.AssemblyName;
        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver)
        {
            if (typeNamespace == ResolverNamespace)
            {
                Type result = DynamicTypeBuilder.Instance.GetDynamicType(typeName);
                if (result != null)
                {
                    return result;
                }
            }

            return knownTypeResolver.ResolveName(typeName, typeNamespace, declaredType, null);
        }

        public override bool TryResolveType(Type type, Type declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            if (type.Assembly.GetName().Name == DynamicTypeBuilder.AssemblyName)
            {
                XmlDictionary dic = new XmlDictionary();
                typeName = dic.Add(type.Name);
                typeNamespace = dic.Add(ResolverNamespace);
                return true;
            }
            else
            {
                return knownTypeResolver.TryResolveType(type, declaredType, null, out typeName, out typeNamespace);
            }
        }
    }
}
