using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Json;
using System.ServiceModel.Channels;
using System.Xml;
using System.IO;
using System.ServiceModel.Description;

namespace JsonRpcOverTcp
{
    static class JsonRpcHelpers
    {
        public const string JsonValueMessageProperty = "MessageAsJsonValue";

        public static JsonObject DeserializeMessage(ref Message message)
        {
            if (message.Properties.ContainsKey(JsonValueMessageProperty))
            {
                return (JsonObject)message.Properties[JsonValueMessageProperty];
            }
            else
            {
                JsonObject json = null;
                byte[] bytes = null;
                using (XmlDictionaryReader bodyReader = message.GetReaderAtBodyContents())
                {
                    bodyReader.ReadStartElement("Binary");
                    bytes = bodyReader.ReadContentAsBase64();
                    json = JsonValue.Load(new MemoryStream(bytes)) as JsonObject;
                }

                if (json == null)
                {
                    throw new ArgumentException("Message must be a JSON object");
                }

                Message newMessage = SerializeMessage(json, message);
                message.Close();
                message = newMessage;
                return json;
            }
        }

        public static Message SerializeMessage(JsonObject json, Message previousMessage)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                json.Save(ms);
                Message result = Message.CreateMessage(MessageVersion.None, null, new ByteStreamBodyWriter(ms.ToArray()));
                if (previousMessage != null)
                {
                    result.Properties.CopyProperties(previousMessage.Properties);
                    result.Headers.CopyHeadersFrom(previousMessage.Headers);
                    previousMessage.Close();
                }

                result.Properties[JsonValueMessageProperty] = json;
                return result;
            }
        }

        public static bool IsUntypedMessage(OperationDescription operation)
        {
            int inputParametersCont = operation.Messages[0].Body.Parts.Count;
            if (inputParametersCont == 1)
            {
                return operation.Messages[0].Body.Parts[0].Type == typeof(Message);
            }
            else if (inputParametersCont == 0)
            {
                Type returnType = operation.Messages[1].Body.ReturnValue.Type;
                return returnType == typeof(void) || returnType == typeof(Message);
            }
            else
            {
                return false;
            }
        }

        class ByteStreamBodyWriter : BodyWriter
        {
            byte[] bytes;
            public ByteStreamBodyWriter(byte[] bytes)
                : base(true)
            {
                this.bytes = bytes;
            }

            protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
            {
                writer.WriteStartElement("Binary");
                writer.WriteBase64(this.bytes, 0, this.bytes.Length);
                writer.WriteEndElement();
            }
        }
    }
}
