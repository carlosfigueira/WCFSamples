using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JsonRpcOverTcp.ServiceModel
{
    static class JsonRpcConstants
    {
        public const string IdKey = "id";
        public const string MethodKey = "method";
        public const string ErrorKey = "error";
        public const string ResultKey = "result";
        public const string ParamsKey = "params";
        public const string RequestIdMessageProperty = "jsonRpcRequestId";
        public const string JObjectMessageProperty = "MessageAsJObject";
    }
}
