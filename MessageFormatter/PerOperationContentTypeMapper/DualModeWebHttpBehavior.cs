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
    public class DualModeWebHttpBehavior : WebHttpBehavior
    {
        protected override IDispatchMessageFormatter GetRequestDispatchFormatter(OperationDescription operationDescription, ServiceEndpoint endpoint)
        {
            IDispatchMessageFormatter originalFormatter = base.GetRequestDispatchFormatter(operationDescription, endpoint);
            return new DualModeFormatter(operationDescription, originalFormatter);
        }
    }
}
