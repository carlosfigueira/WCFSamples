using System.ServiceModel.Channels;

namespace CustomTcpDuplex.Channels
{
    public class SizedTcpDuplexTransportBindingElement : TransportBindingElement
    {
        public const string SizedTcpScheme = "sized.tcp";

        public SizedTcpDuplexTransportBindingElement()
            : base()
        {
        }

        public SizedTcpDuplexTransportBindingElement(SizedTcpDuplexTransportBindingElement other)
            : base(other)
        {
        }

        public override string Scheme
        {
            get { return SizedTcpScheme; }
        }

        public override BindingElement Clone()
        {
            return new SizedTcpDuplexTransportBindingElement(this);
        }

        public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
        {
            return typeof(TChannel) == typeof(IDuplexChannel);
        }

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            return (IChannelFactory<TChannel>)(object)new SizedTcpDuplexChannelFactory(this, context);
        }

        public override bool CanBuildChannelListener<TChannel>(BindingContext context)
        {
            return typeof(TChannel) == typeof(IDuplexChannel);
        }

        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            return (IChannelListener<TChannel>)(object)new SizedTcpDuplexChannelListener(this, context);
        }

        public override T GetProperty<T>(BindingContext context)
        {
            if (typeof(T) == typeof(MessageVersion))
            {
                return (T)(object)MessageVersion.Soap12WSAddressing10;
            }

            return base.GetProperty<T>(context);
        }
    }
}
