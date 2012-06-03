using System;
using System.Globalization;
using System.Runtime.Caching;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading;

namespace OperationSelectorExample
{
    public class CachingOperationInvoker : IOperationInvoker
    {
        private readonly string cacheKeySeparator = Guid.NewGuid().ToString("D");
        IOperationInvoker originalInvoker;
        double cacheDuration;

        public CachingOperationInvoker(IOperationInvoker originalInvoker, double cacheDuration)
        {
            this.originalInvoker = originalInvoker;
            this.cacheDuration = cacheDuration;
        }

        public object[] AllocateInputs()
        {
            return this.originalInvoker.AllocateInputs();
        }

        public object Invoke(object instance, object[] inputs, out object[] outputs)
        {
#if Webhosted
            Cache cache = GetCache();
#else
            ObjectCache cache = this.GetCache();
#endif
            string cacheKey = this.CreateCacheKey(inputs);
            CachedResult cacheItem = cache[cacheKey] as CachedResult;
            if (cacheItem != null)
            {
                outputs = cacheItem.Outputs;
                return cacheItem.ReturnValue;
            }
            else
            {
                object result = this.originalInvoker.Invoke(instance, inputs, out outputs);
                cacheItem = new CachedResult { ReturnValue = result, Outputs = outputs };
#if Webhosted
                cache.Insert(cacheKey, cacheItem, null, Cache.NoAbsoluteExpiration, TimeSpan.FromSeconds(this.cacheDuration));
#else
                cache.Add(cacheKey, cacheItem, DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(this.cacheDuration)));
#endif
                return result;
            }
        }

        public IAsyncResult InvokeBegin(object instance, object[] inputs, AsyncCallback callback, object state)
        {
#if Webhosted
            Cache cache = GetCache();
#else
            ObjectCache cache = this.GetCache();
#endif
            string cacheKey = this.CreateCacheKey(inputs);
            CachedResult cacheItem = cache[cacheKey] as CachedResult;
            CachingUserState cachingUserState = new CachingUserState
            {
                CacheItem = cacheItem,
                CacheKey = cacheKey,
                OriginalUserCallback = callback,
                OriginalUserState = state
            };

            IAsyncResult originalAsyncResult;
            if (cacheItem != null)
            {
                InvokerDelegate invoker = cacheItem.GetValue;
                object[] dummy;
                originalAsyncResult = invoker.BeginInvoke(inputs, out dummy, this.InvokerCallback, cachingUserState);
            }
            else
            {
                originalAsyncResult = this.originalInvoker.InvokeBegin(instance, inputs, this.InvokerCallback, cachingUserState);
            }

            return new CachingAsyncResult(originalAsyncResult, cachingUserState);
        }

        public object InvokeEnd(object instance, out object[] outputs, IAsyncResult asyncResult)
        {
            CachingAsyncResult cachingAsyncResult = asyncResult as CachingAsyncResult;
            CachingUserState cachingUserState = cachingAsyncResult.CachingUserState;
            if (cachingUserState.CacheItem == null)
            {
                object result = this.originalInvoker.InvokeEnd(instance, out outputs, cachingAsyncResult.OriginalAsyncResult);
                cachingUserState.CacheItem = new CachedResult { ReturnValue = result, Outputs = outputs };
#if Webhosted
                this.GetCache().Insert(cachingUserState.CacheKey, cachingUserState.CacheItem, null, Cache.NoAbsoluteExpiration, TimeSpan.FromSeconds(this.cacheDuration));
#else
                this.GetCache().Add(cachingUserState.CacheKey, cachingUserState.CacheItem, DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(this.cacheDuration)));
#endif
                return result;
            }
            else
            {
                InvokerDelegate invoker = ((System.Runtime.Remoting.Messaging.AsyncResult)cachingAsyncResult.OriginalAsyncResult).AsyncDelegate as InvokerDelegate;
                invoker.EndInvoke(out outputs, cachingAsyncResult.OriginalAsyncResult);
                return cachingUserState.CacheItem.ReturnValue;
            }
        }

        public bool IsSynchronous
        {
            get { return this.originalInvoker.IsSynchronous; }
        }

        delegate object InvokerDelegate(object[] inputs, out object[] outputs);

        private void InvokerCallback(IAsyncResult asyncResult)
        {
            CachingUserState cachingUserState = asyncResult.AsyncState as CachingUserState;
            cachingUserState.OriginalUserCallback(new CachingAsyncResult(asyncResult, cachingUserState));
        }

#if Webhosted
        private Cache GetCache()
        {
            return HttpRuntime.Cache;
        }
#else
        private ObjectCache GetCache()
        {
            return MemoryCache.Default;
        }
#endif

        private string CreateCacheKey(object[] inputs)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(this.cacheKeySeparator);
            for (int i = 0; i < inputs.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(this.cacheKeySeparator);
                }

                sb.AppendFormat(CultureInfo.InvariantCulture, "{0}", inputs[i]);
            }

            return sb.ToString();
        }

        class CachingUserState
        {
            public CachedResult CacheItem { get; set; }
            public string CacheKey { get; set; }
            public AsyncCallback OriginalUserCallback { get; set; }
            public object OriginalUserState { get; set; }
        }

        class CachedResult
        {
            public object ReturnValue { get; set; }
            public object[] Outputs { get; set; }

            public object GetValue(object[] inputs, out object[] outputs)
            {
                outputs = this.Outputs;
                return this.ReturnValue;
            }
        }

        class CachingAsyncResult : IAsyncResult
        {
            IAsyncResult originalResult;
            CachingUserState cachingUserState;
            public CachingAsyncResult(IAsyncResult originalResult, CachingUserState cachingUserState)
            {
                this.originalResult = originalResult;
                this.cachingUserState = cachingUserState;
            }

            public object AsyncState
            {
                get { return this.cachingUserState.OriginalUserState; }
            }

            public WaitHandle AsyncWaitHandle
            {
                get { return this.originalResult.AsyncWaitHandle; }
            }

            public bool CompletedSynchronously
            {
                get { return this.originalResult.CompletedSynchronously; }
            }

            public bool IsCompleted
            {
                get { return this.originalResult.IsCompleted; }
            }

            internal CachingUserState CachingUserState
            {
                get { return this.cachingUserState; }
            }

            internal IAsyncResult OriginalAsyncResult
            {
                get { return this.originalResult; }
            }
        }
    }
}
