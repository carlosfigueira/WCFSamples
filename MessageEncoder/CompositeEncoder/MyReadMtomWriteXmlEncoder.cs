using System;
using System.IO;
using System.ServiceModel.Channels;

namespace CompositeEncoder
{
    class MyReadMtomWriteXmlEncoder : MessageEncoder
    {
        MessageEncoder mtom;
        MessageEncoder text;

        public MyReadMtomWriteXmlEncoder(MessageEncoder mtom, MessageEncoder text)
        {
            this.mtom = mtom;
            this.text = text;
        }

        public override string ContentType
        {
            get { return this.text.ContentType; }
        }

        public override string MediaType
        {
            get { return this.text.MediaType; }
        }

        public override MessageVersion MessageVersion
        {
            get { return this.text.MessageVersion; }
        }

        public override bool IsContentTypeSupported(string contentType)
        {
            return this.mtom.IsContentTypeSupported(contentType);
        }

        public override Message ReadMessage(ArraySegment<byte> buffer, BufferManager bufferManager, string contentType)
        {
            return this.mtom.ReadMessage(buffer, bufferManager, contentType);
        }

        public override Message ReadMessage(Stream stream, int maxSizeOfHeaders, string contentType)
        {
            return this.mtom.ReadMessage(stream, maxSizeOfHeaders, contentType);
        }

        public override ArraySegment<byte> WriteMessage(Message message, int maxMessageSize, BufferManager bufferManager, int messageOffset)
        {
            return this.text.WriteMessage(message, maxMessageSize, bufferManager, messageOffset);
        }

        public override void WriteMessage(Message message, Stream stream)
        {
            this.text.WriteMessage(message, stream);
        }
    }
}
