using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.IO;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Net;

namespace PerOperationContentTypeMapper
{
    [ServiceContract]
    public class Service
    {
        [WebInvoke(BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        [NonRaw]
        public int Add(int x, int y)
        {
            return x + y;
        }

        [WebInvoke]
        public string Upload(Stream data)
        {
            return new StreamReader(data).ReadToEnd();
        }
    }
}
