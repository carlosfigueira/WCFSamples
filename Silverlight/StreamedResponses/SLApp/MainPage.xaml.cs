using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;

namespace SLApp
{
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            ServiceReference1.WcfDownloadServiceClient client = new ServiceReference1.WcfDownloadServiceClient("CustomBinding_IWcfDownloadService");
            client.DownloadCompleted += new EventHandler<ServiceReference1.DownloadCompletedEventArgs>(client_DownloadCompleted);
            client.DownloadAsync("1234", 10000);
            this.AddToDebug("Called DownloadAsync");
        }

        void client_DownloadCompleted(object sender, ServiceReference1.DownloadCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                this.AddToDebug("In client_DownloadCompleted, e.Result = {0}", e.Result.Length);
            }
            else
            {
                this.AddToDebug("In client_DownloadCompleted, e.Error = {0}", e.Error);
            }
        }

        private void btnStartStreaming_Click(object sender, RoutedEventArgs e)
        {
            ChannelFactory<SLApp.Web.IWcfDownloadService> factory = new ChannelFactory<Web.IWcfDownloadService>("CustomBinding_IWcfDownloadService_StreamedResponse");
            SLApp.Web.IWcfDownloadService proxy = factory.CreateChannel();
            SLApp.Web.DownloadRequest request = new SLApp.Web.DownloadRequest();
            request.fileName = "test.bin";
            request.fileSize = 1000000000L; // ~1GB
            Message input = Message.CreateMessage(factory.Endpoint.Binding.MessageVersion, SLApp.Web.Constants.DownloadAction, request);
            proxy.BeginDownload(input, new AsyncCallback(this.DownloadCallback), proxy);
            this.AddToDebug("Called proxy.BeginDownload");
        }

        void DownloadCallback(IAsyncResult asyncResult)
        {
            SLApp.Web.IWcfDownloadService proxy = (SLApp.Web.IWcfDownloadService)asyncResult.AsyncState;
            this.AddToDebug("Inside DownloadCallback");
            try
            {
                Message response = proxy.EndDownload(asyncResult);
                this.AddToDebug("Got the response");
                if (response.IsFault)
                {
                    this.AddToDebug("Error in the server: {0}", response);
                }
                else
                {
                    XmlDictionaryReader bodyReader = response.GetReaderAtBodyContents();
                    if (!bodyReader.ReadToDescendant("DownloadResult")) // Name of operation + "Result"
                    {
                        this.AddToDebug("Error, could not read to the start of the result");
                    }
                    else
                    {
                        bodyReader.Read(); // move to content
                        long totalBytesRead = 0;
                        int bytesRead = 0;
                        int i = 0;
                        byte[] buffer = new byte[1000000];
                        do
                        {
                            bytesRead = bodyReader.ReadContentAsBase64(buffer, 0, buffer.Length);
                            totalBytesRead += bytesRead;
                            i++;
                            if ((i % 100) == 0)
                            {
                                this.AddToDebug("Read {0} bytes", totalBytesRead);
                            }
                        } while (bytesRead > 0);

                        this.AddToDebug("Read a total of {0} bytes", totalBytesRead);
                    }
                }
            }
            catch (Exception e)
            {
                this.AddToDebug("Exception: {0}", e);
            }
        }

        void AddToDebug(string format, params object[] args)
        {
            string text = format;
            if (args != null && args.Length > 0)
            {
                text = String.Format(format, args);
            }

            this.Dispatcher.BeginInvoke(() => this.txtDebug.Text = this.txtDebug.Text + text + Environment.NewLine);
        }
    }
}
