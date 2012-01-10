using System;
using System.IO;
using System.ServiceModel.Channels;
using System.Text;

namespace ChangeXmlPrefixes
{
    class LoggingMessageEncodingBindingElement : MessageEncodingBindingElement
    {
        MessageEncodingBindingElement inner;
        public LoggingMessageEncodingBindingElement(MessageEncodingBindingElement inner)
        {
            this.inner = inner;
        }

        private LoggingMessageEncodingBindingElement(LoggingMessageEncodingBindingElement other)
        {
            this.inner = other.inner;
        }

        public override MessageEncoderFactory CreateMessageEncoderFactory()
        {
            return new LoggingMessageEncoderFactory(this.inner.CreateMessageEncoderFactory());
        }

        public override MessageVersion MessageVersion
        {
            get { return this.inner.MessageVersion; }
            set { this.inner.MessageVersion = value; }
        }

        public override BindingElement Clone()
        {
            return new LoggingMessageEncodingBindingElement(this);
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

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            context.BindingParameters.Add(this);
            return context.BuildInnerChannelFactory<TChannel>();
        }

        public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
        {
            return context.CanBuildInnerChannelFactory<TChannel>();
        }

        public static CustomBinding ReplaceEncodingBindingElement(Binding originalBinding)
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
                    LoggingMessageEncodingBindingElement element = new LoggingMessageEncodingBindingElement((MessageEncodingBindingElement)custom.Elements[i]);
                    custom.Elements[i] = element;
                    break;
                }
            }

            return custom;
        }

        class LoggingMessageEncoderFactory : MessageEncoderFactory
        {
            private MessageEncoderFactory messageEncoderFactory;

            public LoggingMessageEncoderFactory(MessageEncoderFactory messageEncoderFactory)
            {
                this.messageEncoderFactory = messageEncoderFactory;
            }

            public override MessageEncoder Encoder
            {
                get { return new LoggingMessageEncoder(this.messageEncoderFactory.Encoder); }
            }

            public override MessageVersion MessageVersion
            {
                get { return this.messageEncoderFactory.MessageVersion; }
            }

            public override MessageEncoder CreateSessionEncoder()
            {
                return new LoggingMessageEncoder(this.messageEncoderFactory.CreateSessionEncoder());
            }
        }

        class LoggingMessageEncoder : MessageEncoder
        {
            private MessageEncoder messageEncoder;

            public LoggingMessageEncoder(MessageEncoder messageEncoder)
            {
                this.messageEncoder = messageEncoder;
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
                string incoming = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
                Console.WriteLine("Incoming message:");
                Console.WriteLine(incoming);
                Console.WriteLine();

                return this.messageEncoder.ReadMessage(buffer, bufferManager, contentType);
            }

            public override Message ReadMessage(Stream stream, int maxSizeOfHeaders, string contentType)
            {
                throw new NotSupportedException("Streamed not supported");
            }

            public override ArraySegment<byte> WriteMessage(Message message, int maxMessageSize, BufferManager bufferManager, int messageOffset)
            {
                return this.messageEncoder.WriteMessage(message, maxMessageSize, bufferManager, messageOffset);
            }

            public override void WriteMessage(Message message, Stream stream)
            {
                throw new NotSupportedException("Streamed not supported");
            }
        }
    }
}
