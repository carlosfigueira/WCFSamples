using System.ServiceModel.Channels;

namespace JsonRpcOverTcp.Channels
{
    public class SizedTcpTransportBindingElement : TransportBindingElement
    {
        public const string SizedTcpScheme = "sized.tcp";

        public SizedTcpTransportBindingElement()
            : base()
        {
        }

        public SizedTcpTransportBindingElement(SizedTcpTransportBindingElement other)
            : base(other)
        {
        }

        public override string Scheme
        {
            get { return SizedTcpScheme; }
        }

        public override BindingElement Clone()
        {
            return new SizedTcpTransportBindingElement(this);
        }

        public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
        {
            return typeof(TChannel) == typeof(IRequestChannel);
        }

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            return (IChannelFactory<TChannel>)(object)new SizedTcpChannelFactory(this, context);
        }

        public override bool CanBuildChannelListener<TChannel>(BindingContext context)
        {
            return typeof(TChannel) == typeof(IReplyChannel);
        }

        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            return (IChannelListener<TChannel>)(object)new SizedTcpChannelListener(this, context);
        }

        public override T GetProperty<T>(BindingContext context)
        {
            if (typeof(T) == typeof(MessageVersion))
            {
                return (T)(object)MessageVersion.None;
            }

            return base.GetProperty<T>(context);
        }
    }
}
