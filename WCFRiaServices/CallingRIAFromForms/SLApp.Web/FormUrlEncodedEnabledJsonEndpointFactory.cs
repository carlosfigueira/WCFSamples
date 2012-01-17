using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Web;
using System.Xml;
using Microsoft.ServiceModel.DomainServices.Hosting;

namespace SLApp.Web
{
    public class FormUrlEncodedEnabledJsonEndpointFactory : JsonEndpointFactory
    {
        public const string FormUrlEncodedInputProperty = "FormUrlEncodedInputProperty";

        public override IEnumerable<ServiceEndpoint> CreateEndpoints(System.ServiceModel.DomainServices.Server.DomainServiceDescription description, System.ServiceModel.DomainServices.Hosting.DomainServiceHost serviceHost)
        {
            List<ServiceEndpoint> endpoints = new List<ServiceEndpoint>(base.CreateEndpoints(description, serviceHost));
            ServiceEndpoint jsonEndpoint = endpoints[0];
            jsonEndpoint.Behaviors.Insert(0, new FormUrlEncodedToJsonEndpointBehavior());
            return endpoints.AsEnumerable();
        }
    }

    class FormUrlEncodedToJsonEndpointBehavior : IEndpointBehavior
    {
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new FormUrlEncodedToJsonInspector(endpoint));
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }
    }

    class FormUrlEncodedToJsonInspector : IDispatchMessageInspector
    {
        ServiceEndpoint endpoint;
        public FormUrlEncodedToJsonInspector(ServiceEndpoint endpoint)
        {
            this.endpoint = endpoint;
        }

        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            HttpRequestMessageProperty reqProp = request.Properties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty;
            if (reqProp.Headers[HttpRequestHeader.ContentType] == "application/x-www-form-urlencoded")
            {
                string operation = request.Headers.To.AbsolutePath;
                int lastSlash = operation.LastIndexOf('/');
                if (lastSlash >= 0)
                {
                    operation = operation.Substring(lastSlash + 1);
                }

                OperationDescription operationDescription = this.endpoint.Contract.Operations.Find(operation);
                if (operationDescription != null && 
                    operationDescription.Behaviors.Find<CanReceiveFormsUrlEncodedInputAttribute>() != null)
                {
                    // Decode the forms-urlencoded input
                    XmlDictionaryReader bodyReader = request.GetReaderAtBodyContents(); 
                    byte[] input = bodyReader.ReadElementContentAsBase64(); 
                    string inputStr = Encoding.UTF8.GetString(input);
                    NameValueCollection parameters = HttpUtility.ParseQueryString(inputStr);

                    // Create an equivalent JSON
                    StringBuilder json = new StringBuilder();
                    json.Append('{');
                    this.ConvertNVCToJson(operationDescription, parameters, json);
                    json.Append('}');

                    // Recreate the message with the JSON input
                    byte[] jsonBytes = Encoding.UTF8.GetBytes(json.ToString());
                    XmlDictionaryReader jsonReader = JsonReaderWriterFactory.CreateJsonReader(jsonBytes, XmlDictionaryReaderQuotas.Max);
                    Message newMessage = Message.CreateMessage(request.Version, null, jsonReader);
                    newMessage.Headers.CopyHeadersFrom(request);
                    newMessage.Properties.CopyProperties(request.Properties);

                    // Notify the application this this change happened
                    OperationContext.Current.IncomingMessageProperties.Add(FormUrlEncodedEnabledJsonEndpointFactory.FormUrlEncodedInputProperty, true);
                    newMessage.Properties.Add(FormUrlEncodedEnabledJsonEndpointFactory.FormUrlEncodedInputProperty, true);

                    // Change the 'raw' input to 'json'
                    newMessage.Properties.Remove(WebBodyFormatMessageProperty.Name);
                    newMessage.Properties.Add(WebBodyFormatMessageProperty.Name, new WebBodyFormatMessageProperty(WebContentFormat.Json));

                    request = newMessage; 
                }
            }

            return null;
        }

        private void ConvertNVCToJson(OperationDescription operationDescription, NameValueCollection parameters, StringBuilder json)
        {
            bool wrapRequest = false;
            string firstParameterName = null;
            if (operationDescription.Messages[0].Body.Parts.Count == 1)
            {
                firstParameterName = operationDescription.Messages[0].Body.Parts[0].Name;
                // special case for inputs of complex types
                if (parameters[firstParameterName] == null)
                {
                    wrapRequest = true;
                }
            }

            if (wrapRequest)
            {
                json.Append("\"" + firstParameterName + "\":{");
            }

            bool first = true;
            foreach (string key in parameters.Keys)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    json.Append(",");
                }

                json.AppendFormat("\"{0}\":\"{1}\"", key, parameters[key]);
            }

            if (wrapRequest)
            {
                json.Append("}");
            }
        }

        public void BeforeSendReply(ref Message reply, object correlationState)
        {
        }
    }
}