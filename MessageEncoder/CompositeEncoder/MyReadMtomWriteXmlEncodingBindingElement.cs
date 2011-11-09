using System;
using System.ServiceModel.Channels;

namespace CompositeEncoder
{
    class MyReadMtomWriteXmlEncodingBindingElement : MessageEncodingBindingElement
    {
        public MtomMessageEncodingBindingElement mtomBE;
        public TextMessageEncodingBindingElement textBE;

        public MyReadMtomWriteXmlEncodingBindingElement()
        {
            this.mtomBE = new MtomMessageEncodingBindingElement();
            this.textBE = new TextMessageEncodingBindingElement();
        }

        public MyReadMtomWriteXmlEncodingBindingElement(MtomMessageEncodingBindingElement mtomBE, TextMessageEncodingBindingElement textBE)
        {
            if (textBE.MessageVersion != mtomBE.MessageVersion)
            {
                throw new ArgumentException("MessageVersion of the two inner encodings must be the same.");
            }

            this.mtomBE = mtomBE;
            this.textBE = textBE;
        }

        public override MessageEncoderFactory CreateMessageEncoderFactory()
        {
            return new MyReadMtomWriteXmlEncoderFactory(
                this.mtomBE.CreateMessageEncoderFactory(),
                this.textBE.CreateMessageEncoderFactory());
        }

        public override MessageVersion MessageVersion
        {
            get { return this.mtomBE.MessageVersion; }
            set
            {
                this.textBE.MessageVersion = value;
                this.mtomBE.MessageVersion = value;
            }
        }

        public override BindingElement Clone()
        {
            return new MyReadMtomWriteXmlEncodingBindingElement(
                (MtomMessageEncodingBindingElement)this.mtomBE.Clone(), 
                (TextMessageEncodingBindingElement)this.textBE.Clone());
        }

        public override bool CanBuildChannelListener<TChannel>(BindingContext context)
        {
            return this.mtomBE.CanBuildChannelListener<TChannel>(context);
        }

        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            context.BindingParameters.Add(this);
            return context.BuildInnerChannelListener<TChannel>();
        }
    }
}
