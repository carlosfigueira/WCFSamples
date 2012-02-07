using System.Globalization;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Threading;

namespace GlobAwareService
{
    class GlobAwareCallContextInitializer : ICallContextInitializer
    {
        public void AfterInvoke(object correlationState)
        {
            CultureInfo culture = correlationState as CultureInfo;
            if (culture != null)
            {
                Thread.CurrentThread.CurrentCulture = culture;
            }
        }

        public object BeforeInvoke(InstanceContext instanceContext, IClientChannel channel, Message message)
        {
            object correlationState = null;

            object prop;
            if (message.Properties.TryGetValue(HttpRequestMessageProperty.Name, out prop))
            {
                var httpProp = prop as HttpRequestMessageProperty;
                string acceptLanguage = httpProp.Headers[HttpRequestHeader.AcceptLanguage];
                CultureInfo requestCulture = null;
                if (!string.IsNullOrEmpty(acceptLanguage))
                {
                    requestCulture = new CultureInfo(acceptLanguage);
                    correlationState = Thread.CurrentThread.CurrentCulture;
                    Thread.CurrentThread.CurrentCulture = requestCulture;
                }
            }

            return correlationState;
        }
    }
}
