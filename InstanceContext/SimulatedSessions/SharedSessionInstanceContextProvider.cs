using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Threading;

namespace SimulatedSessions
{
    class SharedSessionInstanceContextProvider : IInstanceContextProvider, IDispatchMessageInspector
    {
        private readonly static object SyncRoot = new object();
        Dictionary<string, InstanceContext> instanceContexts = new Dictionary<string, InstanceContext>();
        Thread expirationThread;

        public InstanceContext GetExistingInstanceContext(Message message, IContextChannel channel)
        {
            int headerIndex = message.Headers.FindHeader(Constants.HeaderName, Constants.HeaderNamespace);
            if (headerIndex >= 0)
            {
                string instanceId = message.Headers.GetHeader<string>(headerIndex);

                lock (SyncRoot)
                {
                    if (this.instanceContexts.ContainsKey(instanceId))
                    {
                        InstanceContext context = this.instanceContexts[instanceId];
                        SharedInstanceContextInfo info = context.Extensions.Find<SharedInstanceContextInfo>();
                        info.IncrementBusyCount();
                        return context;
                    }
                }
            }

            return null;
        }

        public void InitializeInstanceContext(InstanceContext instanceContext, Message message, IContextChannel channel)
        {
            string instanceId;
            int headerIndex = message.Headers.FindHeader(Constants.HeaderName, Constants.HeaderNamespace);
            if (headerIndex >= 0)
            {
                instanceId = message.Headers.GetHeader<string>(headerIndex);
            }
            else
            {
                instanceId = Guid.NewGuid().ToString();
            }
         
            SharedInstanceContextInfo info = new SharedInstanceContextInfo(instanceContext);
            info.IncrementBusyCount(); // one for the current caller
            info.IncrementBusyCount(); // one for the expiration timer

            instanceContext.Extensions.Add(info);

            lock (SyncRoot)
            {
                this.instanceContexts.Add(instanceId, instanceContext);
                if (this.expirationThread == null)
                {
                    this.expirationThread = new Thread(RemoveExpiredInstanceContexts);
                    this.expirationThread.Start();
                }
            }

            instanceContext.Closing += delegate(object sender, EventArgs e)
            {
                lock (SyncRoot)
                {
                    this.instanceContexts.Remove(instanceId);
                    if (this.instanceContexts.Count == 0)
                    {
                        this.expirationThread.Abort();
                        this.expirationThread = null;
                    }
                }
            };
        }

        private void RemoveExpiredInstanceContexts()
        {
            try
            {
                while (true)
                {
                    lock (SyncRoot)
                    {
                        List<SharedInstanceContextInfo> toRemove = new List<SharedInstanceContextInfo>();

                        foreach (var key in this.instanceContexts.Keys)
                        {
                            InstanceContext context = this.instanceContexts[key];
                            SharedInstanceContextInfo info = context.Extensions.Find<SharedInstanceContextInfo>();
                            toRemove.Add(info);
                        }

                        foreach (var info in toRemove)
                        {
                            if (info.IsExpired())
                            {
                                info.DecrementBusyCount(); // let it get to 0
                            }
                        }
                    }

                    Thread.CurrentThread.Join(1000); // check again in 1 second
                }
            }
            catch (ThreadAbortException) { }
        }

        public bool IsIdle(InstanceContext instanceContext)
        {
            var info = instanceContext.Extensions.Find<SharedInstanceContextInfo>();
            return info.IsIdle;
        }

        public void NotifyIdle(InstanceContextIdleCallback callback, InstanceContext instanceContext)
        {
            SharedInstanceContextInfo info = instanceContext.Extensions.Find<SharedInstanceContextInfo>();
            info.SetIdleCallback(callback);
        }

        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            return instanceContext;
        }

        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            InstanceContext instanceContext = (InstanceContext)correlationState;
            SharedInstanceContextInfo info = instanceContext.Extensions.Find<SharedInstanceContextInfo>();
            info.DecrementBusyCount();
        }
    }

    class SharedInstanceContextInfo : IExtension<InstanceContext>
    {
        internal static readonly int SecondsToIdle = 10;
        DateTime expiration;
        int busyCount;
        InstanceContext instanceContext;
        InstanceContextIdleCallback idleCallback;

        public SharedInstanceContextInfo(InstanceContext instanceContext)
        {
            this.instanceContext = instanceContext;
            this.UpdateExpiration();
            this.busyCount = 0;
        }

        public bool IsIdle
        {
            get;
            private set;
        }

        public DateTime Expiration
        {
            get { return this.expiration; }
        }

        public void UpdateExpiration()
        {
            this.expiration = DateTime.UtcNow.AddSeconds(SecondsToIdle);
        }

        public bool IsExpired()
        {
            return DateTime.UtcNow > this.expiration;
        }

        public void IncrementBusyCount()
        {
            this.busyCount++;
        }

        public void DecrementBusyCount()
        {
            this.busyCount--;
            this.CheckIdle();
        }

        public void SetIdleCallback(InstanceContextIdleCallback callback)
        {
            this.idleCallback = callback;
            this.CheckIdle();
        }

        private void CheckIdle()
        {
            if (this.busyCount == 0 && this.idleCallback != null)
            {
                InstanceContextIdleCallback callback = this.idleCallback;
                this.idleCallback = null;
                if (callback != null)
                {
                    try
                    {
                        this.IsIdle = true;
                        callback(this.instanceContext);
                    }
                    finally
                    {
                        this.IsIdle = false;
                    }
                }
            }
        }

        // Unused in this example
        public void Attach(InstanceContext owner) { }
        public void Detach(InstanceContext owner) { }
    }
}
