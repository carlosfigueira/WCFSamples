using System.ServiceModel.Channels;

namespace CompositeEncoder
{
    class MyReadMtomWriteXmlEncoderFactory : MessageEncoderFactory
    {
        MessageEncoderFactory mtom;
        MessageEncoderFactory text;

        public MyReadMtomWriteXmlEncoderFactory(MessageEncoderFactory mtom, MessageEncoderFactory text)
        {
            this.mtom = mtom;
            this.text = text;
        }

        public override MessageEncoder Encoder
        {
            get { return new MyReadMtomWriteXmlEncoder(this.mtom.Encoder, this.text.Encoder); }
        }

        public override MessageVersion MessageVersion
        {
            get { return this.mtom.MessageVersion; }
        }
    }
}
