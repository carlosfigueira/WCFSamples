using System.ServiceModel;
using System.ServiceModel.Channels;

namespace SharingDataThroughPipeline
{
    class MyChannelExtension : IExtension<IContextChannel>
    {
        public Binding Binding { get; set; }
        public bool IntroduceErrors { get; set; }

        // Not used in this scenario
        public void Attach(IContextChannel owner) { }
        public void Detach(IContextChannel owner) { }
    }
}
