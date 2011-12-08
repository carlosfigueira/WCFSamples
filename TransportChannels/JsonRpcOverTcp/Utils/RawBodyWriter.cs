using System.ServiceModel.Channels;
using System.Xml;

namespace JsonRpcOverTcp.Utils
{
    public class RawBodyWriter : BodyWriter
    {
        byte[] bytes;
        public RawBodyWriter(byte[] bytes)
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
