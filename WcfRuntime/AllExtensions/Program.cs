using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace AllExtensions
{
    class MyDispatchMessageInspector : IDispatchMessageInspector
    {
        public MyDispatchMessageInspector()
        {
        }

        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            ColorConsole.WriteLine(ConsoleColor.Cyan, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
            return null;
        }

        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            ColorConsole.WriteLine(ConsoleColor.Cyan, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
        }
    }

    class MyDispatchMessageFormatter : IDispatchMessageFormatter
    {
        IDispatchMessageFormatter inner;
        public MyDispatchMessageFormatter(IDispatchMessageFormatter inner)
        {
            this.inner = inner;
        }

        public void DeserializeRequest(Message message, object[] parameters)
        {
            ColorConsole.WriteLine(ConsoleColor.Cyan, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
            this.inner.DeserializeRequest(message, parameters);
        }

        public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            ColorConsole.WriteLine(ConsoleColor.Cyan, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
            return this.inner.SerializeReply(messageVersion, parameters, result);
        }
    }

    class MyClientMessageInspector : IClientMessageInspector
    {
        public MyClientMessageInspector()
        {
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            ColorConsole.WriteLine(ConsoleColor.Yellow, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            ColorConsole.WriteLine(ConsoleColor.Yellow, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
            return null;
        }
    }

    class MyClientMessageFormatter : IClientMessageFormatter
    {
        IClientMessageFormatter inner;
        public MyClientMessageFormatter(IClientMessageFormatter inner)
        {
            this.inner = inner;
        }

        public object DeserializeReply(Message message, object[] parameters)
        {
            ColorConsole.WriteLine(ConsoleColor.Yellow, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
            return this.inner.DeserializeReply(message, parameters);
        }

        public Message SerializeRequest(MessageVersion messageVersion, object[] parameters)
        {
            ColorConsole.WriteLine(ConsoleColor.Yellow, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
            return this.inner.SerializeRequest(messageVersion, parameters);
        }
    }

    class MyDispatchOperationSelector : IDispatchOperationSelector
    {
        public string SelectOperation(ref Message message)
        {
            ColorConsole.WriteLine(ConsoleColor.Cyan, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
            string action = message.Headers.Action;
            string method = action.Substring(action.LastIndexOf('/') + 1);
            return method;
        }
    }

    class MyParameterInspector : IParameterInspector
    {
        ConsoleColor consoleColor;
        bool isServer;
        public MyParameterInspector(bool isServer)
        {
            this.isServer = isServer;
            this.consoleColor = isServer ? ConsoleColor.Cyan : ConsoleColor.Yellow;
        }

        public void AfterCall(string operationName, object[] outputs, object returnValue, object correlationState)
        {
            ColorConsole.WriteLine(this.consoleColor, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
        }

        public object BeforeCall(string operationName, object[] inputs)
        {
            ColorConsole.WriteLine(this.consoleColor, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
            return null;
        }
    }

    class MyCallContextInitializer : ICallContextInitializer
    {
        public void AfterInvoke(object correlationState)
        {
            ColorConsole.WriteLine(ConsoleColor.Cyan, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
        }

        public object BeforeInvoke(InstanceContext instanceContext, IClientChannel channel, Message message)
        {
            ColorConsole.WriteLine(ConsoleColor.Cyan, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
            return null;
        }
    }

    class MyOperationInvoker : IOperationInvoker
    {
        IOperationInvoker inner;
        public MyOperationInvoker(IOperationInvoker inner)
        {
            this.inner = inner;
        }

        public object[] AllocateInputs()
        {
            ColorConsole.WriteLine(ConsoleColor.Cyan, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
            return this.inner.AllocateInputs();
        }

        public object Invoke(object instance, object[] inputs, out object[] outputs)
        {
            ColorConsole.WriteLine(ConsoleColor.Cyan, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
            return this.inner.Invoke(instance, inputs, out outputs);
        }

        public IAsyncResult InvokeBegin(object instance, object[] inputs, AsyncCallback callback, object state)
        {
            ColorConsole.WriteLine(ConsoleColor.Cyan, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
            return this.inner.InvokeBegin(instance, inputs, callback, state);
        }

        public object InvokeEnd(object instance, out object[] outputs, IAsyncResult result)
        {
            ColorConsole.WriteLine(ConsoleColor.Cyan, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
            return this.inner.InvokeEnd(instance, out outputs, result);
        }

        public bool IsSynchronous
        {
            get
            {
                ColorConsole.WriteLine(ConsoleColor.Cyan, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
                return this.inner.IsSynchronous;
            }
        }
    }

    class MyInstanceProvider : IInstanceProvider
    {
        Type serviceType;
        public MyInstanceProvider(Type serviceType)
        {
            this.serviceType = serviceType;
        }

        public object GetInstance(InstanceContext instanceContext, Message message)
        {
            ColorConsole.WriteLine(ConsoleColor.Cyan, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
            return Activator.CreateInstance(this.serviceType);
        }

        public object GetInstance(InstanceContext instanceContext)
        {
            ColorConsole.WriteLine(ConsoleColor.Cyan, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
            return Activator.CreateInstance(this.serviceType);
        }

        public void ReleaseInstance(InstanceContext instanceContext, object instance)
        {
            ColorConsole.WriteLine(ConsoleColor.Cyan, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
        }
    }

    class MyInstanceContextProvider : IInstanceContextProvider
    {
        IInstanceContextProvider inner;
        public MyInstanceContextProvider(IInstanceContextProvider inner)
        {
            this.inner = inner;
        }

        public InstanceContext GetExistingInstanceContext(Message message, IContextChannel channel)
        {
            ColorConsole.WriteLine(ConsoleColor.Cyan, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
            return this.inner.GetExistingInstanceContext(message, channel);
        }

        public void InitializeInstanceContext(InstanceContext instanceContext, Message message, IContextChannel channel)
        {
            ColorConsole.WriteLine(ConsoleColor.Cyan, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
            this.inner.InitializeInstanceContext(instanceContext, message, channel);
        }

        public bool IsIdle(InstanceContext instanceContext)
        {
            ColorConsole.WriteLine(ConsoleColor.Cyan, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
            return this.inner.IsIdle(instanceContext);
        }

        public void NotifyIdle(InstanceContextIdleCallback callback, InstanceContext instanceContext)
        {
            ColorConsole.WriteLine(ConsoleColor.Cyan, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
            this.inner.NotifyIdle(callback, instanceContext);
        }
    }

    class MyInstanceContextInitializer : IInstanceContextInitializer
    {
        public void Initialize(InstanceContext instanceContext, Message message)
        {
            ColorConsole.WriteLine(ConsoleColor.Cyan, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
        }
    }

    class MyChannelInitializer : IChannelInitializer
    {
        ConsoleColor consoleColor;
        public MyChannelInitializer(bool isServer)
        {
            this.consoleColor = isServer ? ConsoleColor.Cyan : ConsoleColor.Yellow;
        }
        public void Initialize(IClientChannel channel)
        {
            ColorConsole.WriteLine(this.consoleColor, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
        }
    }

    class MyClientOperationSelector : IClientOperationSelector
    {
        public bool AreParametersRequiredForSelection
        {
            get
            {
                ColorConsole.WriteLine(ConsoleColor.Yellow, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
                return false;
            }
        }

        public string SelectOperation(MethodBase method, object[] parameters)
        {
            ColorConsole.WriteLine(ConsoleColor.Yellow, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
            MethodInfo methodInfo = method as MethodInfo;
            if (methodInfo != null)
            {
                if (methodInfo.ReturnType == typeof(IAsyncResult) && methodInfo.Name.StartsWith("Begin"))
                {
                    OperationContractAttribute oca = methodInfo
                        .GetCustomAttributes(typeof(OperationContractAttribute), false)
                        .FirstOrDefault()
                        as OperationContractAttribute;
                    if (oca != null && oca.AsyncPattern)
                    {
                        return method.Name.Substring("Begin".Length);
                    }
                }
                else
                {
                    ParameterInfo[] methodParams = method.GetParameters();
                    if (methodParams.Length == 1 && methodParams[0].ParameterType == typeof(IAsyncResult) && method.Name.StartsWith("End"))
                    {
                        return method.Name.Substring("End".Length);
                    }
                }
            }

            return method.Name;
        }
    }

    class MyInteractiveChannelInitializer : IInteractiveChannelInitializer
    {
        public IAsyncResult BeginDisplayInitializationUI(IClientChannel channel, AsyncCallback callback, object state)
        {
            ColorConsole.WriteLine(ConsoleColor.Yellow, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
            Action act = new Action(this.DoNothing);
            return act.BeginInvoke(callback, state);
        }

        public void EndDisplayInitializationUI(IAsyncResult result)
        {
            ColorConsole.WriteLine(ConsoleColor.Yellow, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
        }

        private void DoNothing() { }
    }

    class MyErrorHandler : IErrorHandler
    {
        public bool HandleError(Exception error)
        {
            ColorConsole.WriteLine(ConsoleColor.Cyan, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
            return error is ArgumentException;
        }

        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {
            ColorConsole.WriteLine(ConsoleColor.Cyan, "{0}.{1}", this.GetType().Name, ReflectionUtil.GetMethodSignature(MethodBase.GetCurrentMethod()));
            MessageFault messageFault = MessageFault.CreateFault(new FaultCode("FaultCode"), new FaultReason(error.Message));
            fault = Message.CreateMessage(version, messageFault, "FaultAction");
        }
    }

    [ServiceContract]
    public interface ITest
    {
        [OperationContract]
        int Add(int x, int y);
        [OperationContract(IsOneWay = true)]
        void Process(string text);
        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginEcho(string text, AsyncCallback callback, object state);
        string EndEcho(IAsyncResult result);
    }

    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class Service : ITest
    {
        public int Add(int x, int y)
        {
            ColorConsole.WriteLine(ConsoleColor.Green, "In service operation '{0}'", MethodBase.GetCurrentMethod().Name);

            if (x == 0 && y == 0)
            {
                throw new ArgumentException("This will cause IErrorHandler to be called");
            }
            else
            {
                return x + y;
            }
        }

        public void Process(string text)
        {
            ColorConsole.WriteLine(ConsoleColor.Green, "In service operation '{0}'", MethodBase.GetCurrentMethod().Name);
        }

        public IAsyncResult BeginEcho(string text, AsyncCallback callback, object state)
        {
            ColorConsole.WriteLine(ConsoleColor.Green, "In service operation '{0}'", MethodBase.GetCurrentMethod().Name);
            Func<string, string> func = x => x;
            return func.BeginInvoke(text, callback, state);
        }

        public string EndEcho(IAsyncResult result)
        {
            ColorConsole.WriteLine(ConsoleColor.Green, "In service operation '{0}'", MethodBase.GetCurrentMethod().Name);
            Func<string, string> func = (Func<string, string>)
                ((System.Runtime.Remoting.Messaging.AsyncResult)result).AsyncDelegate;
            return func.EndInvoke(result);
        }
    }

    class MyBehavior : IOperationBehavior, IContractBehavior
    {
        public void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
        {
            clientOperation.Formatter = new MyClientMessageFormatter(clientOperation.Formatter);
            clientOperation.ParameterInspectors.Add(new MyParameterInspector(false));
        }

        public void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
        {
            dispatchOperation.CallContextInitializers.Add(new MyCallContextInitializer());
            dispatchOperation.Formatter = new MyDispatchMessageFormatter(dispatchOperation.Formatter);
            dispatchOperation.Invoker = new MyOperationInvoker(dispatchOperation.Invoker);
            dispatchOperation.ParameterInspectors.Add(new MyParameterInspector(true));
        }

        public void Validate(OperationDescription operationDescription)
        {
        }

        public void AddBindingParameters(ContractDescription contractDescription, ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ContractDescription contractDescription, ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.ChannelInitializers.Add(new MyChannelInitializer(false));
            clientRuntime.InteractiveChannelInitializers.Add(new MyInteractiveChannelInitializer());
            clientRuntime.MessageInspectors.Add(new MyClientMessageInspector());
            clientRuntime.OperationSelector = new MyClientOperationSelector();
        }

        public void ApplyDispatchBehavior(ContractDescription contractDescription, ServiceEndpoint endpoint, DispatchRuntime dispatchRuntime)
        {
            dispatchRuntime.ChannelDispatcher.ChannelInitializers.Add(new MyChannelInitializer(true));
            dispatchRuntime.ChannelDispatcher.ErrorHandlers.Add(new MyErrorHandler());
            dispatchRuntime.InstanceContextInitializers.Add(new MyInstanceContextInitializer());
            dispatchRuntime.InstanceContextProvider = new MyInstanceContextProvider(dispatchRuntime.InstanceContextProvider);
            dispatchRuntime.InstanceProvider = new MyInstanceProvider(dispatchRuntime.ChannelDispatcher.Host.Description.ServiceType);
            dispatchRuntime.MessageInspectors.Add(new MyDispatchMessageInspector());
            dispatchRuntime.OperationSelector = new MyDispatchOperationSelector();
        }

        public void Validate(ContractDescription contractDescription, ServiceEndpoint endpoint)
        {
        }
    }

    static class ColorConsole
    {
        static object syncRoot = new object();

        public static void WriteLine(ConsoleColor color, string text, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                text = string.Format(CultureInfo.InvariantCulture, text, args);
            }

            lock (syncRoot)
            {
                Console.ForegroundColor = color;
                Console.WriteLine("[{0}] {1}", DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture), text);
                Console.ResetColor();
            }

            Thread.Sleep(50);
        }

        public static void WriteLine(string text, params object[] args)
        {
            Console.WriteLine(text, args);
        }

        public static void WriteLine(object obj)
        {
            Console.WriteLine(obj);
        }
    }

    static class ReflectionUtil
    {
        public static string GetMethodSignature(MethodBase method)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(method.Name);
            sb.Append("(");
            ParameterInfo[] parameters = method.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(parameters[i].ParameterType.Name);
            }
            sb.Append(")");
            return sb.ToString();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string baseAddress = "http://" + Environment.MachineName + ":8000/Service";
            using (ServiceHost host = new ServiceHost(typeof(Service), new Uri(baseAddress)))
            {
                ServiceEndpoint endpoint = host.AddServiceEndpoint(typeof(ITest), new BasicHttpBinding(), "");
                endpoint.Contract.Behaviors.Add(new MyBehavior());
                foreach (OperationDescription operation in endpoint.Contract.Operations)
                {
                    operation.Behaviors.Add(new MyBehavior());
                }

                host.Open();
                ColorConsole.WriteLine("Host opened");

                using (ChannelFactory<ITest> factory = new ChannelFactory<ITest>(new BasicHttpBinding(), new EndpointAddress(baseAddress)))
                {
                    factory.Endpoint.Contract.Behaviors.Add(new MyBehavior());
                    foreach (OperationDescription operation in factory.Endpoint.Contract.Operations)
                    {
                        operation.Behaviors.Add(new MyBehavior());
                    }

                    ITest proxy = factory.CreateChannel();
                    ColorConsole.WriteLine("Calling operation");
                    ColorConsole.WriteLine(proxy.Add(3, 4));

                    Console.WriteLine();
                    Console.WriteLine("   *-*-*-*-*-*-*-*-*-*-*-*-*-*-*");
                    Console.WriteLine();

                    ColorConsole.WriteLine("Called operation, calling it again, this time it the service will throw");
                    try
                    {
                        ColorConsole.WriteLine(proxy.Add(0, 0));
                    }
                    catch (Exception e)
                    {
                        ColorConsole.WriteLine(ConsoleColor.Red, "{0}: {1}", e.GetType().Name, e.Message);
                    }

                    Console.WriteLine();
                    Console.WriteLine("   *-*-*-*-*-*-*-*-*-*-*-*-*-*-*");
                    Console.WriteLine();

                    ColorConsole.WriteLine("Now calling an OneWay operation");
                    proxy.Process("hello");
                    Thread.Sleep(5000);

                    Console.WriteLine();
                    Console.WriteLine("   *-*-*-*-*-*-*-*-*-*-*-*-*-*-*");
                    Console.WriteLine();

                    ManualResetEvent evt = new ManualResetEvent(false);
                    ColorConsole.WriteLine("Now calling an asynchronous operation");
                    proxy.BeginEcho("hello", delegate(IAsyncResult ar)
                    {
                        Console.WriteLine(proxy.EndEcho(ar));
                        evt.Set();
                    }, null);

                    evt.WaitOne();

                    ((IClientChannel)proxy).Close();
                }
            }

            ColorConsole.WriteLine("Done");
        }
    }
}
