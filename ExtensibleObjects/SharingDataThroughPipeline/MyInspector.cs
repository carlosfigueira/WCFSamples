using System.Globalization;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Xml;

namespace SharingDataThroughPipeline
{
    class MyInspector : IClientMessageInspector, IEndpointBehavior
    {
        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            MyChannelExtension extension = channel.Extensions.Find<MyChannelExtension>();
            if (extension != null && extension.IntroduceErrors)
            {
                this.IntroduceErrorToMessage(ref request);
            }

            return null;
        }

        private void IntroduceErrorToMessage(ref Message message)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(message.GetReaderAtBodyContents());

            XmlNamespaceManager nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace("tempuri", "http://tempuri.org/");
            XmlElement xNode = doc.SelectSingleNode("//tempuri:x", nsManager) as XmlElement;
            XmlText xValue = xNode.FirstChild as XmlText;
            xValue.Value = (double.Parse(xValue.Value, CultureInfo.InvariantCulture) + 1).ToString(CultureInfo.InvariantCulture);

            MemoryStream ms = new MemoryStream();
            XmlWriterSettings writerSettings = new XmlWriterSettings
            {
                CloseOutput = false,
                OmitXmlDeclaration = true,
                Encoding = Encoding.UTF8,
            };

            XmlWriter writer = XmlWriter.Create(ms, writerSettings);
            doc.WriteTo(writer);
            writer.Close();
            ms.Position = 0;
            XmlReader reader = XmlReader.Create(ms);

            Message newMessage = Message.CreateMessage(message.Version, null, reader);
            newMessage.Headers.CopyHeadersFrom(message);
            newMessage.Properties.CopyProperties(message.Properties);
            message.Close();
            message = newMessage;
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.MessageInspectors.Add(this);
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }
    }
}
