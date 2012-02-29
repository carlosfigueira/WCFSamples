using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.IO;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Net;

namespace PerOperationContentTypeMapper
{
    public class DualModeFormatter : IDispatchMessageFormatter
    {
        OperationDescription operation;
        IDispatchMessageFormatter originalFormatter;
        MessageEncoder webMessageEncoder;
        BufferManager bufferManager;

        public DualModeFormatter(OperationDescription operation, IDispatchMessageFormatter originalFormatter)
        {
            this.operation = operation;
            this.originalFormatter = originalFormatter;
            this.webMessageEncoder = new WebMessageEncodingBindingElement()
                .CreateMessageEncoderFactory()
                .Encoder;
            this.bufferManager = BufferManager.CreateBufferManager(int.MaxValue, int.MaxValue);
        }

        public void DeserializeRequest(Message message, object[] parameters)
        {
            if (this.operation.Behaviors.Find<NonRawAttribute>() != null)
            {
                ArraySegment<byte> buffer = this.webMessageEncoder.WriteMessage(message, int.MaxValue, bufferManager);
                string contentType = ((HttpRequestMessageProperty)message.Properties[HttpRequestMessageProperty.Name])
                    .Headers[HttpRequestHeader.ContentType];
                message = this.webMessageEncoder.ReadMessage(buffer, bufferManager, contentType);
                bufferManager.ReturnBuffer(buffer.Array);
            }

            this.originalFormatter.DeserializeRequest(message, parameters);
        }

        public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            throw new NotSupportedException("This is a request-only formatter");
        }
    }
}
