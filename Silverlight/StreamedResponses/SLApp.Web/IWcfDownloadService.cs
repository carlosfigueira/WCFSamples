using System;
using System.IO;
#if SILVERLIGHT
using System.Runtime.Serialization;
#endif
using System.ServiceModel;
#if !SILVERLIGHT
using System.ServiceModel.Activation;
#endif
using System.ServiceModel.Channels;

namespace SLApp.Web
{
    [ServiceContract(Namespace = "")]
    public interface IWcfDownloadService
    {
#if !SILVERLIGHT
        [OperationContract(Action = Constants.DownloadAction, ReplyAction = Constants.DownloadReplyAction)]
        Stream Download(string fileName, long fileSize);
#else
        [OperationContract(AsyncPattern = true, Action = Constants.DownloadAction, ReplyAction = Constants.DownloadReplyAction)]
        IAsyncResult BeginDownload(Message request, AsyncCallback callback, object state);
        Message EndDownload(IAsyncResult asyncResult);
#endif
    }

    public static class Constants
    {
        public const string DownloadAction = "http://my.company.com/download";
        public const string DownloadReplyAction = "http://my.company.com/download";
    }

#if SILVERLIGHT
    [DataContract(Name = "Download", Namespace = "")] // same namespace as the [ServiceContract], same name as operation
    public class DownloadRequest
    {
        [DataMember(Order = 1)]
        public string fileName;
        [DataMember(Order = 2)]
        public long fileSize;
    }
#endif
}
