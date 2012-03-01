using System;
using System.Collections.Generic;
using System.IO;
using System.Json;
using System.Runtime.Serialization.Json;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;
using System.Text;
using System.Xml;

namespace SLApp
{
    public class WebMessageEncodingBindingElement : MessageEncodingBindingElement
    {
        public override MessageEncoderFactory CreateMessageEncoderFactory()
        {
            return new WebMessageEncoderFactory();
        }

        public override MessageVersion MessageVersion
        {
            get
            {
                return MessageVersion.None;
            }
            set
            {
                if (value != MessageVersion.None) throw new ArgumentException("Only MV.None is supported");
            }
        }

        public override BindingElement Clone()
        {
            return new WebMessageEncodingBindingElement();
        }

        public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
        {
            return context.CanBuildInnerChannelFactory<TChannel>();
        }

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            context.BindingParameters.Add(this);
            return context.BuildInnerChannelFactory<TChannel>();
        }

        class WebMessageEncoderFactory : MessageEncoderFactory
        {
            WebMessageEncoder encoder = new WebMessageEncoder();
            public override MessageEncoder Encoder
            {
                get { return encoder; }
            }

            public override MessageVersion MessageVersion
            {
                get { return MessageVersion.None; }
            }
        }

        class WebMessageEncoder : MessageEncoder
        {
            MessageEncoder xmlEncoder = new TextMessageEncodingBindingElement(MessageVersion.None, Encoding.UTF8).CreateMessageEncoderFactory().Encoder;
            public override string ContentType
            {
                get { return xmlEncoder.ContentType; }
            }

            public override string MediaType
            {
                get { return xmlEncoder.MediaType; }
            }

            public override MessageVersion MessageVersion
            {
                get { return MessageVersion.None; }
            }

            public override bool IsContentTypeSupported(string contentType)
            {
                return this.xmlEncoder.IsContentTypeSupported(contentType) || contentType.Contains("/json"); // text/json, application/json
            }

            public override Message ReadMessage(ArraySegment<byte> buffer, BufferManager bufferManager, string contentType)
            {
                if (this.xmlEncoder.IsContentTypeSupported(contentType))
                {
                    return this.xmlEncoder.ReadMessage(buffer, bufferManager, contentType);
                }

                Message result = Message.CreateMessage(MessageVersion.None, null, new RawBodyWriter(buffer));
                result.Properties.Add(WebBodyFormatMessageProperty.Name, new WebBodyFormatMessageProperty(WebContentFormat.Json));
                return result;
            }

            public override Message ReadMessage(Stream stream, int maxSizeOfHeaders, string contentType)
            {
                throw new NotSupportedException("Streamed transfer is not supported by this encoder");
            }

            public override ArraySegment<byte> WriteMessage(Message message, int maxMessageSize, BufferManager bufferManager, int messageOffset)
            {
                bool useRawEncoder = false;
                if (message.Properties.ContainsKey(WebBodyFormatMessageProperty.Name))
                {
                    WebBodyFormatMessageProperty prop = (WebBodyFormatMessageProperty)message.Properties[WebBodyFormatMessageProperty.Name];
                    useRawEncoder = prop.Format == WebContentFormat.Json || prop.Format == WebContentFormat.Raw;
                }

                if (useRawEncoder)
                {
                    MemoryStream ms = new MemoryStream();
                    XmlDictionaryReader reader = message.GetReaderAtBodyContents();
                    byte[] buffer = reader.ReadElementContentAsBase64();
                    byte[] managedBuffer = bufferManager.TakeBuffer(buffer.Length + messageOffset);
                    Array.Copy(buffer, 0, managedBuffer, messageOffset, buffer.Length);
                    return new ArraySegment<byte>(managedBuffer, messageOffset, buffer.Length);
                }
                else
                {
                    return this.xmlEncoder.WriteMessage(message, maxMessageSize, bufferManager, messageOffset);
                }
            }

            public override void WriteMessage(Message message, Stream stream)
            {
                throw new NotSupportedException("Streamed transfer is not supported by this encoder");
            }
        }
    }
    class RawBodyWriter : BodyWriter
    {
        ArraySegment<byte> buffer;
        Stream stream;
        public RawBodyWriter(ArraySegment<byte> buffer)
            : base(true)
        {
            this.buffer = buffer;
        }

        public RawBodyWriter(byte[] buffer, int offset, int count)
            : base(true)
        {
            this.buffer = new ArraySegment<byte>(buffer, offset, count);
        }

        public RawBodyWriter(byte[] buffer) : this(buffer, 0, buffer.Length) { }

        public RawBodyWriter(Stream stream)
            : base(false)
        {
            this.stream = stream;
        }

        bool IsStreamed
        {
            get { return this.stream != null; }
        }

        protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
        {
            writer.WriteStartElement("Binary");
            if (IsStreamed)
            {
                byte[] buffer = new byte[1000];
                int bytesRead;
                do
                {
                    bytesRead = this.stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        writer.WriteBase64(buffer, 0, bytesRead);
                    }
                } while (bytesRead > 0);
            }
            else
            {
                writer.WriteBase64(this.buffer.Array, this.buffer.Offset, this.buffer.Count);
            }
            writer.WriteEndElement();
        }
    }
    public class WebHttpBehaviorWithJson : WebHttpBehavior
    {
        protected override IClientMessageFormatter GetRequestClientFormatter(OperationDescription operationDescription, ServiceEndpoint endpoint)
        {
            if (GetRequestFormat(operationDescription) == WebMessageFormat.Json)
            {
                JsonClientFormatter jsonFormatter = new JsonClientFormatter(endpoint.Address.Uri, operationDescription, this.DefaultBodyStyle);
                return jsonFormatter;
            }
            else
            {
                return base.GetRequestClientFormatter(operationDescription, endpoint);
            }
        }

        protected override IClientMessageFormatter GetReplyClientFormatter(OperationDescription operationDescription, ServiceEndpoint endpoint)
        {
            IClientMessageFormatter xmlFormatter = base.GetReplyClientFormatter(operationDescription, endpoint);
            IClientMessageFormatter jsonFormatter = new JsonClientFormatter(endpoint.Address.Uri, operationDescription, this.DefaultBodyStyle);
            return new JsonOrXmlReplyFormatter(xmlFormatter, jsonFormatter);
        }

        WebMessageFormat GetRequestFormat(OperationDescription od)
        {
            WebGetAttribute wga = od.Behaviors.Find<WebGetAttribute>();
            WebInvokeAttribute wia = od.Behaviors.Find<WebInvokeAttribute>();
            if (wga != null && wia != null)
            {
                throw new InvalidOperationException("Only 1 of [WebGet] or [WebInvoke] can be applied to each operation");
            }

            if (wga != null)
            {
                return wga.RequestFormat;
            }

            if (wia != null)
            {
                return wia.RequestFormat;
            }

            return this.DefaultOutgoingRequestFormat;
        }
    }
    class JsonOrXmlReplyFormatter : IClientMessageFormatter
    {
        IClientMessageFormatter jsonFormatter;
        IClientMessageFormatter xmlFormatter;

        public JsonOrXmlReplyFormatter(IClientMessageFormatter xmlFormatter, IClientMessageFormatter jsonFormatter)
        {
            this.xmlFormatter = xmlFormatter;
            this.jsonFormatter = jsonFormatter;
        }

        #region IClientMessageFormatter Members

        public object DeserializeReply(Message message, object[] parameters)
        {
            object prop;
            if (message.Properties.TryGetValue(WebBodyFormatMessageProperty.Name, out prop))
            {
                WebBodyFormatMessageProperty format = (WebBodyFormatMessageProperty)prop;
                if (format.Format == WebContentFormat.Json)
                {
                    return this.jsonFormatter.DeserializeReply(message, parameters);
                }
            }

            return this.xmlFormatter.DeserializeReply(message, parameters);
        }

        public Message SerializeRequest(MessageVersion messageVersion, object[] parameters)
        {
            throw new NotImplementedException("This is a reply-only formatter");
        }

        #endregion
    }

    class JsonClientFormatter : IClientMessageFormatter
    {
        Uri baseUri;
        OperationDescription operationDescription;
        WebMessageBodyStyle defaultBodyStyle;
        QueryStringConverter queryStringConverter;

        const string HttpMethodGet = "GET";
        const string HttpMethodHead = "HEAD";
        const string HttpMethodPost = "POST";

        UriTemplate uriTemplate;
        string method;
        Dictionary<int, string> pathMapping;
        Dictionary<int, KeyValuePair<Type, string>> queryMapping;

        public JsonClientFormatter(Uri baseUri, OperationDescription operationDescription, WebMessageBodyStyle defaultBodyStyle)
        {
            this.baseUri = baseUri;
            this.operationDescription = operationDescription;
            this.defaultBodyStyle = defaultBodyStyle;
            this.queryStringConverter = new QueryStringConverter();
            this.Initialize();
        }

        private void Initialize()
        {
            this.uriTemplate = GetUriTemplate(this.operationDescription);
            this.method = GetHttpRequestMethod(this.operationDescription);
            List<string> pathVariables = new List<string>(this.uriTemplate.PathSegmentVariableNames);
            List<string> queryVariables = new List<string>(this.uriTemplate.QueryValueVariableNames);
            int numberOfUriTemplateVars = pathVariables.Count + queryVariables.Count;
            this.pathMapping = new Dictionary<int, string>();
            this.queryMapping = new Dictionary<int, KeyValuePair<Type, string>>();

            for (int i = 0; i < this.operationDescription.Messages[0].Body.Parts.Count; i++)
            {
                MessagePartDescription part = operationDescription.Messages[0].Body.Parts[i];
                string parameterName = part.Name;
                List<string> pathCopy = new List<string>(pathVariables);
                foreach (string pathVar in pathCopy)
                {
                    if (String.Compare(pathVar, parameterName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        if (part.Type != typeof(string))
                        {
                            throw new InvalidOperationException(
                                String.Format(
                                    "UriTemplate path variable '{0}' for operation '{1}' needs to be of type String",
                                    pathVar,
                                    operationDescription.Name));
                        }
                        pathMapping.Add(i, parameterName);
                        pathVariables.Remove(pathVar);
                    }
                }

                List<string> queryCopy = new List<string>(queryVariables);
                foreach (string queryVar in queryCopy)
                {
                    if (String.Compare(queryVar, parameterName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        if (!queryStringConverter.CanConvert(part.Type))
                        {
                            throw new InvalidOperationException(
                                String.Format(
                                    "Parameter '{0}' of type '{1}' cannot be converted to strings by the QueryStringConverter",
                                    parameterName,
                                    part.Type.FullName));
                        }

                        queryMapping.Add(i, new KeyValuePair<Type, string>(part.Type, parameterName));
                        queryVariables.Remove(queryVar);
                    }
                }
            }

            if (pathVariables.Count > 0)
            {
                throw new InvalidOperationException(
                    String.Format(
                        "Operation '{0}' expects a parameter named '{1}', but such parameter doesn't exist",
                        operationDescription.Name,
                        pathVariables[0]));
            }

            if (queryVariables.Count > 0)
            {
                throw new InvalidOperationException(
                    String.Format(
                        "Operation '{0}' expects a parameter named '{1}', but such parameter doesn't exist",
                        operationDescription.Name,
                        queryVariables[0]));
            }

            int numberOfUnmappedParameters = operationDescription.Messages[0].Body.Parts.Count - numberOfUriTemplateVars;
            if (numberOfUnmappedParameters > 0 && (this.method == HttpMethodGet || this.method == HttpMethodHead))
            {
                throw new InvalidOperationException(
                    String.Format(
                        "Operation '{0}' has parameters which are not mapped to the UriTemplate, but it cannot have a body (because of the HTTP verb)",
                        operationDescription.Name));
            }

            WebMessageBodyStyle bodyStyle = GetBodyStyle(operationDescription);
            if (numberOfUnmappedParameters > 1)
            {
                if (bodyStyle == WebMessageBodyStyle.Bare || bodyStyle == WebMessageBodyStyle.WrappedResponse)
                {
                    throw new InvalidOperationException(
                        String.Format(
                            "Operation '{0}' has more than 1 parameter to be serialized without a wrapper element. Use BodyStyle.Wrapped (or WrappedRequest) for this operation",
                            operationDescription.Name));
                }
            }

            if (operationDescription.Messages.Count > 1 && operationDescription.Messages[1].Body.Parts.Count > 1)
            {
                if (bodyStyle == WebMessageBodyStyle.Bare || bodyStyle == WebMessageBodyStyle.WrappedRequest)
                {
                    throw new InvalidOperationException(
                        String.Format(
                            "Operation '{0}' has more than 1 output parameter to be serialized without a wrapper element. Use BodyStyle.Wrapped (or WrappedResponse) for this operation",
                            operationDescription.Name));
                }
            }
        }

        #region IClientMessageFormatter Members

        public object DeserializeReply(Message message, object[] parameters)
        {
            if (this.operationDescription.Messages.Count == 1)
            {
                return null;
            }

            if (parameters.Length == 0 && this.operationDescription.Messages[1].Body.ReturnValue.Type == typeof(void))
            {
                return null;
            }

            XmlDictionaryReader reader = message.GetReaderAtBodyContents();
            byte[] buffer = reader.ReadElementContentAsBase64();
            MemoryStream jsonStream = new MemoryStream(buffer);
            WebMessageBodyStyle bodyStyle = GetBodyStyle(this.operationDescription);
            if (bodyStyle == WebMessageBodyStyle.Bare || bodyStyle == WebMessageBodyStyle.WrappedRequest)
            {
                DataContractJsonSerializer dcjs = new DataContractJsonSerializer(this.operationDescription.Messages[1].Body.ReturnValue.Type);
                return dcjs.ReadObject(jsonStream);
            }
            else
            {
                JsonObject jo = JsonValue.Load(jsonStream) as JsonObject;
                if (jo == null)
                {
                    throw new InvalidOperationException("Response is not a JSON object");
                }

                for (int i = 0; i < this.operationDescription.Messages[1].Body.Parts.Count; i++)
                {
                    MessagePartDescription outPart = this.operationDescription.Messages[1].Body.Parts[i];
                    if (jo.ContainsKey(outPart.Name))
                    {
                        parameters[i] = Deserialize(outPart.Type, jo[outPart.Name]);
                    }
                }

                MessagePartDescription returnPart = this.operationDescription.Messages[1].Body.ReturnValue;
                if (returnPart != null && jo.ContainsKey(returnPart.Name))
                {
                    return Deserialize(returnPart.Type, jo[returnPart.Name]);
                }
                else
                {
                    return null;
                }
            }
        }

        static object Deserialize(Type type, JsonValue jv)
        {
            if (jv == null) return null;
            DataContractJsonSerializer dcjs = new DataContractJsonSerializer(type);
            MemoryStream ms = new MemoryStream();
            jv.Save(ms);
            ms.Position = 0;
            return dcjs.ReadObject(ms);
        }

        public Message SerializeRequest(MessageVersion messageVersion, object[] parameters)
        {
            Dictionary<string, string> templateParameters = new Dictionary<string, string>();
            List<KeyValuePair<int, object>> bodyParameters = new List<KeyValuePair<int, object>>();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (pathMapping.ContainsKey(i))
                {
                    templateParameters.Add(pathMapping[i], parameters[i] as string);
                }
                else if (queryMapping.ContainsKey(i))
                {
                    if (parameters[i] != null)
                    {
                        templateParameters.Add(queryMapping[i].Value, queryStringConverter.ConvertValueToString(parameters[i], queryMapping[i].Key));
                    }
                }
                else
                {
                    bodyParameters.Add(new KeyValuePair<int, object>(i, parameters[i]));
                }
            }

            HttpRequestMessageProperty httpRequest = new HttpRequestMessageProperty();
            httpRequest.Method = this.method;
            httpRequest.Headers[System.Net.HttpRequestHeader.ContentType] = "application/json";
            Message result;
            if (this.method == HttpMethodGet || this.method == HttpMethodHead)
            {
                httpRequest.SuppressEntityBody = true;
                result = Message.CreateMessage(MessageVersion.None, null);
            }
            else
            {
                byte[] body = GetJsonBody(bodyParameters);
                result = Message.CreateMessage(MessageVersion.None, null, new RawBodyWriter(body));
            }

            result.Properties.Add(HttpRequestMessageProperty.Name, httpRequest);
            result.Headers.To = uriTemplate.BindByName(baseUri, templateParameters);

            result.Properties.Add(WebBodyFormatMessageProperty.Name, new WebBodyFormatMessageProperty(WebContentFormat.Json));

            return result;
        }

        private byte[] GetJsonBody(List<KeyValuePair<int, object>> bodyParameters)
        {
            WebMessageBodyStyle bodyStyle = GetBodyStyle(this.operationDescription);
            DataContractJsonSerializer dcjs;
            MemoryStream ms;
            if (bodyParameters.Count == 0)
            {
                return new byte[0];
            }

            if (bodyStyle == WebMessageBodyStyle.Bare || bodyStyle == WebMessageBodyStyle.WrappedResponse)
            {
                ms = new MemoryStream();
                dcjs = new DataContractJsonSerializer(this.operationDescription.Messages[0].Body.Parts[bodyParameters[0].Key].Type);
                dcjs.WriteObject(ms, bodyParameters[0].Value);
            }
            else
            {
                JsonObject jo = new JsonObject();
                for (int i = 0; i < bodyParameters.Count; i++)
                {
                    MessagePartDescription part = this.operationDescription.Messages[0].Body.Parts[bodyParameters[i].Key];
                    dcjs = new DataContractJsonSerializer(part.Type);
                    ms = new MemoryStream();
                    dcjs.WriteObject(ms, bodyParameters[i].Value);
                    ms.Position = 0;
                    JsonValue jv = JsonValue.Load(ms);
                    jo.Add(part.Name, jv);
                }

                ms = new MemoryStream();
                jo.Save(ms);
            }

            return ms.ToArray();
        }

        #endregion

        static string GetHttpRequestMethod(OperationDescription od)
        {
            WebGetAttribute wga = od.Behaviors.Find<WebGetAttribute>();
            WebInvokeAttribute wia = od.Behaviors.Find<WebInvokeAttribute>();
            if (wga != null && wia != null)
            {
                throw new InvalidOperationException("Only 1 of [WebGet] or [WebInvoke] can be applied to each operation");
            }

            if (wga != null)
            {
                return HttpMethodGet;
            }

            if (wia != null && wia.Method != null)
            {
                return wia.Method;
            }

            return HttpMethodPost;
        }

        WebMessageBodyStyle GetBodyStyle(OperationDescription od)
        {
            WebGetAttribute wga = od.Behaviors.Find<WebGetAttribute>();
            WebInvokeAttribute wia = od.Behaviors.Find<WebInvokeAttribute>();
            if (wga != null && wia != null)
            {
                throw new InvalidOperationException("Only 1 of [WebGet] or [WebInvoke] can be applied to each operation");
            }

            if (wga != null)
            {
                return wga.BodyStyle;
            }

            if (wia != null)
            {
                return wia.BodyStyle;
            }

            return this.defaultBodyStyle;
        }

        static UriTemplate GetUriTemplate(OperationDescription operation)
        {
            string template = GetWebUriTemplate(operation);
            if (template == null && GetHttpRequestMethod(operation) == HttpMethodGet)
            {
                template = CreateDefaultGetTemplate(operation);
            }

            if (template == null)
            {
                template = operation.Name;
            }

            return new UriTemplate(template);
        }

        static string CreateDefaultGetTemplate(OperationDescription operation)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(operation.Name);
            sb.Append("?");
            foreach (MessagePartDescription part in operation.Messages[0].Body.Parts)
            {
                sb.Append(part.Name);
                sb.Append("={");
                sb.Append(part.Name);
                sb.Append("}&");
            }
            sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }

        static string GetWebUriTemplate(OperationDescription od)
        {
            WebGetAttribute wga = od.Behaviors.Find<WebGetAttribute>();
            WebInvokeAttribute wia = od.Behaviors.Find<WebInvokeAttribute>();
            if (wga != null && wia != null)
            {
                throw new InvalidOperationException("Only 1 of [WebGet] or [WebInvoke] can be applied to each operation");
            }

            if (wga != null)
            {
                return wga.UriTemplate;
            }

            if (wia != null)
            {
                return wia.UriTemplate;
            }

            return null;
        }
    }
}
