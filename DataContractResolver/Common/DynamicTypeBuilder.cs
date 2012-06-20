using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;

namespace Common
{
    public class DynamicTypeBuilder
    {
        internal const string AssemblyName = "ReflectionEmitAssembly";
        private static DynamicTypeBuilder instance;
        private static ModuleBuilder myModule;

        private Dictionary<string, Type> typeCache = new Dictionary<string, Type>();
        private Dictionary<string, Func<Type>> typeCreators = new Dictionary<string, Func<Type>>
        {
            { "Person", () => CreatePersonType() },
            { "Address", () => CreateAddressType() },
            { "Product", () => CreateProductType() },
        };

        static DynamicTypeBuilder()
        {
            AppDomain myDomain = AppDomain.CurrentDomain;
            AssemblyName myAsmName = new AssemblyName(AssemblyName);
            AssemblyBuilder myAssembly =
                myDomain.DefineDynamicAssembly(myAsmName,
                    AssemblyBuilderAccess.RunAndSave);

            myModule =
                myAssembly.DefineDynamicModule(myAsmName.Name,
                   myAsmName.Name + ".dll");
        }

        private DynamicTypeBuilder()
        {
        }

        public static DynamicTypeBuilder Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DynamicTypeBuilder();
                }

                return instance;
            }
        }

        public Type GetDynamicType(string typeName)
        {
            Type result = null;
            if (this.typeCache.ContainsKey(typeName))
            {
                result = this.typeCache[typeName];
            }
            else if (this.typeCreators.ContainsKey(typeName))
            {
                result = this.typeCreators[typeName]();
                this.typeCache.Add(typeName, result);
            }

            return result;
        }

        private static Type CreatePersonType()
        {
            return CreateType(
                "Person",
                new string[] { "name", "age" },
                new string[] { "Name", "Age" },
                new Type[] { typeof(string), typeof(int) });
        }

        private static Type CreateAddressType()
        {
            return CreateType(
                "Address",
                new string[] { "street", "city", "zip" },
                new string[] { "Street", "City", "Zip" },
                new Type[] { typeof(string), typeof(string), typeof(string) });
        }

        private static Type CreateProductType()
        {
            return CreateType(
                "Product",
                new string[] { "id", "name", "price" },
                new string[] { "Id", "Name", "Price" },
                new Type[] { typeof(int), typeof(string), typeof(decimal) });
        }

        private static Type CreateType(string typeName, string[] fieldNames, string[] propertyNames, Type[] propertyTypes)
        {
            // public class Address
            TypeBuilder typeBuilder =
                myModule.DefineType(typeName, TypeAttributes.Public);

            // Add [DataContract] to the type
            CustomAttributeBuilder dataContractBuilder = new CustomAttributeBuilder(
                typeof(DataContractAttribute).GetConstructor(Type.EmptyTypes),
                new object[0]);
            typeBuilder.SetCustomAttribute(dataContractBuilder);

            List<PropertyBuilder> propertyBuilders = new List<PropertyBuilder>();
            // Define fields and properties
            for (int i = 0; i < fieldNames.Length; i++)
            {
                FieldBuilder field =
                    typeBuilder.DefineField(
                        fieldNames[i],
                        propertyTypes[i],
                        FieldAttributes.Private);

                propertyBuilders.Add(CreateProperty(typeBuilder, propertyNames[i], field));
            }

            // Override ToString
            OverrideToString(typeBuilder, propertyBuilders.ToArray());

            return typeBuilder.CreateType();
        }

        private static void OverrideToString(TypeBuilder typeBuilder, params PropertyBuilder[] properties)
        {
            if (properties == null || properties.Length < 1 || properties.Length > 3)
            {
                throw new NotImplementedException("Only implemented for types with 1-3 properties");
            }

            MethodBuilder toString = typeBuilder.DefineMethod(
                "ToString",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                typeof(string),
                Type.EmptyTypes);

            List<Type> stringFormatArgs = new List<Type>();
            stringFormatArgs.Add(typeof(string));
            for (int i = 0; i < properties.Length; i++)
            {
                stringFormatArgs.Add(typeof(object));
            }

            MethodInfo stringFormat = typeof(string).GetMethod(
                "Format",
                BindingFlags.Static | BindingFlags.Public,
                null,
                stringFormatArgs.ToArray(),
                null);

            ILGenerator toStringIL = toString.GetILGenerator();
            StringBuilder sbFormat = new StringBuilder();
            sbFormat.Append(typeBuilder.Name);
            sbFormat.Append('[');
            for (int i = 0; i < properties.Length; i++)
            {
                if (i > 0)
                {
                    sbFormat.Append(',');
                }

                sbFormat.Append(properties[i].Name);
                sbFormat.Append("={");
                sbFormat.Append(i);
                sbFormat.Append("}");
            }

            sbFormat.Append(']');

            toStringIL.Emit(OpCodes.Ldstr, sbFormat.ToString());
            foreach (var property in properties)
            {
                toStringIL.Emit(OpCodes.Ldarg_0);
                toStringIL.Emit(OpCodes.Call, property.GetGetMethod());
                if (property.PropertyType.IsValueType)
                {
                    toStringIL.Emit(OpCodes.Box, property.PropertyType);
                }
            }

            toStringIL.Emit(OpCodes.Call, stringFormat);
            toStringIL.Emit(OpCodes.Ret);
        }

        private static PropertyBuilder CreateProperty(TypeBuilder typeBuilder, string propertyName, FieldBuilder backingField)
        {
            Type propertyType = backingField.FieldType;
            PropertyBuilder property = typeBuilder.DefineProperty(
                propertyName,
                PropertyAttributes.None,
                propertyType,
                Type.EmptyTypes);

            CustomAttributeBuilder dataMemberAttribute = new CustomAttributeBuilder(
                typeof(DataMemberAttribute).GetConstructor(Type.EmptyTypes),
                new object[0]);
            property.SetCustomAttribute(dataMemberAttribute);

            MethodAttributes getSetAttr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

            MethodBuilder getMethod = typeBuilder.DefineMethod("get_" + propertyName, getSetAttr, propertyType, Type.EmptyTypes);
            MethodBuilder setMethod = typeBuilder.DefineMethod("set_" + propertyName, getSetAttr, null, new Type[] { propertyType });

            ILGenerator getIL = getMethod.GetILGenerator();
            getIL.Emit(OpCodes.Ldarg_0);
            getIL.Emit(OpCodes.Ldfld, backingField);
            getIL.Emit(OpCodes.Ret);

            ILGenerator setIL = setMethod.GetILGenerator();
            setIL.Emit(OpCodes.Ldarg_0);
            setIL.Emit(OpCodes.Ldarg_1);
            setIL.Emit(OpCodes.Stfld, backingField);
            setIL.Emit(OpCodes.Ret);

            property.SetSetMethod(setMethod);
            property.SetGetMethod(getMethod);

            return property;
        }
    }
}
