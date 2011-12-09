using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            string inputJsonString = Encoding.UTF8.GetString(input, offset, count);
            JObject outputJson = null;
            using (MemoryStream ms = new MemoryStream(input, offset, count))
            {
                using (JsonReader jsonReader = new JsonTextReader(new StreamReader(ms, Encoding.UTF8)))
                {
                    JObject inputJson = JObject.Load(jsonReader);
                    string methodName = inputJson["method"].Value<string>();
                    MethodInfo method = this.operations[methodName];
                    ParameterInfo[] parameterInfos = method.GetParameters();
                    object[] args = new object[parameterInfos.Length];
                    for (int i = 0; i < args.Length; i++)
                    {
                        args[i] = JsonConvert.DeserializeObject(inputJson["params"][i].ToString(), parameterInfos[i].ParameterType);
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

                    outputJson = new JObject();
                    outputJson.Add("result", result == null ? null : JToken.FromObject(result));
                    outputJson["error"] = null;
                    JObject parent = outputJson;
                    if (error != null)
                    {
                        JObject temp = new JObject();
                        parent["error"] = temp;
                        while (error != null)
                        {
                            temp.Add("type", error.GetType().FullName);
                            temp.Add("message", error.Message);
                            if (error.InnerException != null)
                            {
                                parent = temp;
                                temp = new JObject();
                                parent.Add("inner", temp);
                            }

                            error = error.InnerException;
                        }
                    }

                    outputJson.Add("id", inputJson["id"]);
                }
            }

            using (MemoryStream ms = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(ms))
                {
                    using (JsonTextWriter jtw = new JsonTextWriter(sw))
                    {
                        outputJson.WriteTo(jtw);
                        jtw.Flush();
                        sw.Flush();
                        return ms.ToArray();
                    }
                }
            }
        }
    }
}
