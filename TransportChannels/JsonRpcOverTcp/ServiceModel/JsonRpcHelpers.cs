using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Description;
using System.ServiceModel.Channels;
using Newtonsoft.Json.Linq;
using System.IO;
using Newtonsoft.Json;
using JsonRpcOverTcp.Utils;
using System.Xml;

namespace JsonRpcOverTcp.ServiceModel
{
    static class JsonRpcHelpers
    {
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

        public static Message SerializeMessage(JObject json, Message previousMessage)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(ms))
                {
                    using (JsonTextWriter jtw = new JsonTextWriter(sw))
                    {
                        json.WriteTo(jtw);
                        jtw.Flush();
                        sw.Flush();
                        Message result = Message.CreateMessage(MessageVersion.None, null, new RawBodyWriter(ms.ToArray()));
                        if (previousMessage != null)
                        {
                            result.Properties.CopyProperties(previousMessage.Properties);
                            result.Headers.CopyHeadersFrom(previousMessage.Headers);
                            previousMessage.Close();
                        }

                        result.Properties[JsonRpcConstants.JObjectMessageProperty] = json;
                        return result;
                    }
                }
            }
        }

        public static JObject DeserializeMessage(Message message)
        {
            if (message.Properties.ContainsKey(JsonRpcConstants.JObjectMessageProperty))
            {
                return (JObject)message.Properties[JsonRpcConstants.JObjectMessageProperty];
            }
            else
            {
                JObject json = null;
                byte[] bytes = null;
                using (XmlDictionaryReader bodyReader = message.GetReaderAtBodyContents())
                {
                    bodyReader.ReadStartElement("Binary");
                    bytes = bodyReader.ReadContentAsBase64();
                }

                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    using (StreamReader sr = new StreamReader(ms))
                    {
                        using (JsonTextReader jtr = new JsonTextReader(sr))
                        {
                            json = JObject.Load(jtr);
                        }
                    }
                }

                if (json == null)
                {
                    throw new ArgumentException("Message must be a JSON object");
                }

                return json;
            }
        }
    }
}
