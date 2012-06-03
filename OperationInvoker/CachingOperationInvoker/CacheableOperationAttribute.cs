using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace OperationSelectorExample
{
    public class CacheableOperationAttribute : Attribute, IOperationBehavior
    {
        double secondsToCache;
        public CacheableOperationAttribute()
        {
            this.secondsToCache = 30;
        }

        public double SecondsToCache
        {
            get { return this.secondsToCache; }
            set { this.secondsToCache = value; }
        }

        public void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
        {
        }

        public void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
        {
            dispatchOperation.Invoker = new CachingOperationInvoker(dispatchOperation.Invoker, this.secondsToCache);
        }

        public void Validate(OperationDescription operationDescription)
        {
        }
    }
}
