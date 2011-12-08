using System;
using System.ServiceModel.Channels;
using System.Xml;

namespace JsonRpcOverTcp.Utils
{
    public static class Formatting
    {
        public static int BytesToSize(byte[] bytes, int offset)
        {
            return bytes[offset] << 24 |
                bytes[offset + 1] << 16 |
                bytes[offset + 2] << 8 |
                bytes[offset + 3];
        }

        public static void SizeToBytes(int size, byte[] bytes, int offset)
        {
            bytes[offset + 0] = (byte)(size >> 24);
            bytes[offset + 1] = (byte)(size >> 16);
            bytes[offset + 2] = (byte)(size >> 8);
            bytes[offset + 3] = (byte)(size);
        }

        public static Message BytesToMessage(byte[] bytes)
        {
            return Message.CreateMessage(MessageVersion.None, null, new RawBodyWriter(bytes));
        }

        public static byte[] MessageToBytes(Message rawMessage)
        {
            XmlDictionaryReader bodyReader = rawMessage.GetReaderAtBodyContents();
            bodyReader.MoveToContent();
            if (bodyReader.NodeType == XmlNodeType.Element && bodyReader.Name == "Binary")
            {
                bodyReader.Read();
                return bodyReader.ReadContentAsBase64();
            }
            else
            {
                throw new ArgumentException(
                    string.Format(
                        "Message is not a binary message: current node: {0} - {1} - {2}",
                        bodyReader.NodeType,
                        bodyReader.Name,
                        bodyReader.Value));
            }
        }
    }
}
