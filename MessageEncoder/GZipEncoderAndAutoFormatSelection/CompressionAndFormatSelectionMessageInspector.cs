using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;
using System.Text.RegularExpressions;

namespace GZipEncoderAndAutoFormatSelection
{
    public class CompressionAndFormatSelectionEndpointBehavior : IEndpointBehavior
    {
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new CompressionAndFormatSelectionMessageInspector());
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }
    }

    public class CompressionAndFormatSelectionMessageInspector : IDispatchMessageInspector
    {
        static readonly Regex jsonContentTypes = new Regex(@"[application|text]\/json");
        static readonly Regex xmlContentTypes = new Regex(@"[application|text]\/xml");

        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            object propObj;
            if (request.Properties.TryGetValue(HttpRequestMessageProperty.Name, out propObj))
            {
                var prop = (HttpRequestMessageProperty)propObj;
                var accept = prop.Headers[HttpRequestHeader.Accept];
                if (accept != null)
                {
                    if (jsonContentTypes.IsMatch(accept))
                    {
                        WebOperationContext.Current.OutgoingResponse.Format = WebMessageFormat.Json;
                    }
                    else if (xmlContentTypes.IsMatch(accept))
                    {
                        WebOperationContext.Current.OutgoingResponse.Format = WebMessageFormat.Xml;
                    }
                }
            }

            return null;
        }

        public void BeforeSendReply(ref Message reply, object correlationState)
        {
        }
    }
}
