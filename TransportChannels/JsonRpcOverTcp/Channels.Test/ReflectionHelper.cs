using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace JsonRpcOverTcp.Channels.Test
{
    static class ReflectionHelper
    {
        static readonly BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        public static object CreateInstance(Type publicTypeFromAssembly, string typeName, params object[] constructorArgs)
        {
            Type type = publicTypeFromAssembly.Assembly.GetType(typeName);
            ConstructorInfo ctor = FindConstructor(type, constructorArgs);
            return ctor.Invoke(constructorArgs);
        }

        public static object CallMethod(object obj, string methodName, params object[] methodArgs)
        {
            MethodInfo method = FindMethod(obj.GetType(), methodName, methodArgs);
            return method.Invoke(obj, methodArgs);
        }

        public static void SetField(object obj, string fieldName, object fieldValue)
        {
            FieldInfo field = obj.GetType().GetField(fieldName, InstanceFlags);
            field.SetValue(obj, fieldValue);
        }

        static ConstructorInfo FindConstructor(Type type, object[] arguments)
        {
            return (ConstructorInfo)Filter(
                type.GetConstructors(InstanceFlags),
                ".ctor",
                arguments);
        }

        static MethodInfo FindMethod(Type type, string methodName, object[] arguments)
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return (MethodInfo)Filter(methods, methodName, arguments);
        }

        static MethodBase Filter(IEnumerable<MethodBase> methods, string methodName, object[] arguments)
        {
            var result = methods.Where(m => m.Name == methodName);
            result = result.Where(m => m.GetParameters().Length == arguments.Length);
            if (result.Count() > 1)
            {
                result = result.Where(m =>
                {
                    ParameterInfo[] methodParams = m.GetParameters();
                    for (int i = 0; i < arguments.Length; i++)
                    {
                        if (arguments[i] != null)
                        {
                            Type argType = arguments[i].GetType();
                            if (argType != methodParams[i].ParameterType)
                            {
                                return false;
                            }
                        }
                    }

                    return true;
                });
            }

            return result.First();
        }
    }
}
