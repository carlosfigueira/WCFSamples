using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel.Channels;
using System.Xml;

namespace ChangeXmlPrefixes
{
    public class ReplacePrefixMessageEncodingBindingElement : MessageEncodingBindingElement
    {
        MessageEncodingBindingElement inner;
        Dictionary<string, string> namespaceToPrefixMapping = new Dictionary<string, string>();
        public ReplacePrefixMessageEncodingBindingElement(MessageEncodingBindingElement inner)
        {
            this.inner = inner;
        }

        private ReplacePrefixMessageEncodingBindingElement(ReplacePrefixMessageEncodingBindingElement other)
        {
            this.inner = other.inner;
            this.namespaceToPrefixMapping = new Dictionary<string, string>(other.namespaceToPrefixMapping);
        }

        public void AddNamespaceMapping(string namespaceUri, string newPrefix)
        {
            this.namespaceToPrefixMapping.Add(namespaceUri, newPrefix);
        }

        public override MessageEncoderFactory CreateMessageEncoderFactory()
        {
            return new ReplacePrefixMessageEncoderFactory(this.inner.CreateMessageEncoderFactory(), this.namespaceToPrefixMapping);
        }

        public override MessageVersion MessageVersion
        {
            get { return this.inner.MessageVersion; }
            set { this.inner.MessageVersion = value; }
        }

        public override BindingElement Clone()
        {
            return new ReplacePrefixMessageEncodingBindingElement(this);
        }

        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            context.BindingParameters.Add(this);
            return context.BuildInnerChannelListener<TChannel>();
        }

        public override bool CanBuildChannelListener<TChannel>(BindingContext context)
        {
            return context.CanBuildInnerChannelListener<TChannel>();
        }

        public static CustomBinding ReplaceEncodingBindingElement(Binding originalBinding, Dictionary<string, string> namespaceToPrefixMapping)
        {
            CustomBinding custom = originalBinding as CustomBinding;
            if (custom == null)
            {
                custom = new CustomBinding(originalBinding);
            }

            for (int i = 0; i < custom.Elements.Count; i++)
            {
                if (custom.Elements[i] is MessageEncodingBindingElement)
                {
                    ReplacePrefixMessageEncodingBindingElement element = new ReplacePrefixMessageEncodingBindingElement((MessageEncodingBindingElement)custom.Elements[i]);
                    foreach (var mapping in namespaceToPrefixMapping)
                    {
                        element.AddNamespaceMapping(mapping.Key, mapping.Value);
                    }

                    custom.Elements[i] = element;
                    break;
                }
            }

            return custom;
        }

        class ReplacePrefixMessageEncoderFactory : MessageEncoderFactory
        {
            private MessageEncoderFactory messageEncoderFactory;
            private Dictionary<string, string> namespaceToNewPrefixMapping;

            public ReplacePrefixMessageEncoderFactory(MessageEncoderFactory messageEncoderFactory, Dictionary<string, string> namespaceToNewPrefixMapping)
            {
                this.messageEncoderFactory = messageEncoderFactory;
                this.namespaceToNewPrefixMapping = namespaceToNewPrefixMapping;
            }

            public override MessageEncoder Encoder
            {
                get { return new ReplacePrefixMessageEncoder(this.messageEncoderFactory.Encoder, this.namespaceToNewPrefixMapping); }
            }

            public override MessageVersion MessageVersion
            {
                get { return this.messageEncoderFactory.MessageVersion; }
            }

            public override MessageEncoder CreateSessionEncoder()
            {
                return new ReplacePrefixMessageEncoder(this.messageEncoderFactory.CreateSessionEncoder(), this.namespaceToNewPrefixMapping);
            }
        }

        class ReplacePrefixMessageEncoder : MessageEncoder
        {
            private MessageEncoder messageEncoder;
            private Dictionary<string, string> namespaceToNewPrefixMapping;

            public ReplacePrefixMessageEncoder(MessageEncoder messageEncoder, Dictionary<string, string> namespaceToNewPrefixMapping)
            {
                this.messageEncoder = messageEncoder;
                this.namespaceToNewPrefixMapping = namespaceToNewPrefixMapping;
            }

            public override string ContentType
            {
                get { return this.messageEncoder.ContentType; }
            }

            public override string MediaType
            {
                get { return this.messageEncoder.MediaType; }
            }

            public override MessageVersion MessageVersion
            {
                get { return this.messageEncoder.MessageVersion; }
            }

            public override bool IsContentTypeSupported(string contentType)
            {
                return this.messageEncoder.IsContentTypeSupported(contentType);
            }

            public override Message ReadMessage(ArraySegment<byte> buffer, BufferManager bufferManager, string contentType)
            {
                return this.messageEncoder.ReadMessage(buffer, bufferManager, contentType);
            }

            public override Message ReadMessage(Stream stream, int maxSizeOfHeaders, string contentType)
            {
                throw new NotSupportedException("Streamed not supported");
            }

            public override ArraySegment<byte> WriteMessage(Message message, int maxMessageSize, BufferManager bufferManager, int messageOffset)
            {
                MemoryStream ms = new MemoryStream();
                XmlDictionaryWriter w = XmlDictionaryWriter.CreateBinaryWriter(ms);
                message.WriteMessage(w);
                w.Flush();
                ms.Position = 0;
                XmlDictionaryReader r = XmlDictionaryReader.CreateBinaryReader(ms, XmlDictionaryReaderQuotas.Max);
                XmlDocument doc = new XmlDocument();
                doc.Load(r);
                PrefixReplacer replacer = new PrefixReplacer();
                foreach (var mapping in this.namespaceToNewPrefixMapping)
                {
                    replacer.AddNamespace(mapping.Key, mapping.Value);
                }

                replacer.ChangePrefixes(doc);
                ms = new MemoryStream();
                w = XmlDictionaryWriter.CreateBinaryWriter(ms);
                doc.WriteTo(w);
                w.Flush();
                ms.Position = 0;
                r = XmlDictionaryReader.CreateBinaryReader(ms, XmlDictionaryReaderQuotas.Max);
                Message newMessage = Message.CreateMessage(r, maxMessageSize, message.Version);
                newMessage.Properties.CopyProperties(message.Properties);
                return this.messageEncoder.WriteMessage(newMessage, maxMessageSize, bufferManager, messageOffset);
            }

            public override void WriteMessage(Message message, Stream stream)
            {
                throw new NotSupportedException("Streamed not supported");
            }
        }
    }
}
