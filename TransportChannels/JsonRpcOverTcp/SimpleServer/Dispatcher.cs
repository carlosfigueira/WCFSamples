using System;
using System.Collections.Generic;
using System.IO;
using System.Json;
using System.Reflection;
using System.Runtime.Serialization.Json;

namespace JsonRpcOverTcp.SimpleServer
{
    class Dispatcher
    {
        Dictionary<string, MethodInfo> operations;
        object serviceImplementation;
        public Dispatcher(object serviceImplementation)
        {
            this.InitializeOperationCache(serviceImplementation);
        }

        private void InitializeOperationCache(object serviceImplementation)
        {
            this.serviceImplementation = serviceImplementation;
            this.operations = new Dictionary<string, MethodInfo>();
            Type serviceType = serviceImplementation.GetType();
            foreach (var method in serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!method.IsGenericMethod && !method.IsGenericMethodDefinition && !method.IsAbstract)
                {
                    this.operations.Add(method.Name, method);
                }
            }
        }

        public byte[] DispatchOperation(byte[] input, int offset, int count)
        {
            JsonObject inputJson = JsonValue.Load(new MemoryStream(input, offset, count)) as JsonObject;
            if (inputJson == null)
            {
                throw new ArgumentException("Input is not a JSON object");
            }

            string methodName = inputJson["method"].ReadAs<string>();
            MethodInfo method = this.operations[methodName];
            ParameterInfo[] parameterInfos = method.GetParameters();
            object[] args = new object[parameterInfos.Length];
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = inputJson["params"][i].ReadAs(parameterInfos[i].ParameterType);
            }
            Exception error = null;
            object result = null;
            try
            {
                result = method.Invoke(this.serviceImplementation, args);
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException)
                {
                    e = e.InnerException;
                }

                error = e;
            }

            JsonObject outputJson = new JsonObject();
            outputJson.Add("result", JsonValueExtensions.CreateFrom(result));
            outputJson["error"] = null;
            JsonObject parent = outputJson;
            if (error != null)
            {
                JsonObject temp = new JsonObject();
                parent["error"] = temp;
                while (error != null)
                {
                    temp.Add("type", error.GetType().FullName);
                    temp.Add("message", error.Message);
                    if (error.InnerException != null)
                    {
                        parent = temp;
                        temp = new JsonObject();
                        parent.Add("inner", temp);
                    }

                    error = error.InnerException;
                }
            }

            outputJson.Add("id", inputJson["id"]);
            MemoryStream ms = new MemoryStream();
            outputJson.Save(ms);
            return ms.ToArray();
        }
    }
}
