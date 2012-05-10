using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;

namespace CorsEnabledService
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class ValuesService : IValues
    {
        static Dictionary<string, string> allValues = new Dictionary<string, string> { { "0", "value1" }, { "1", "value2" } };
        static int nextId = 2;

        public List<string> GetValues()
        {
            WebOperationContext.Current.OutgoingResponse.Headers[HttpResponseHeader.CacheControl] = "no-cache";
            return allValues.Values.ToList();
        }

        public string GetValue(string id)
        {
            WebOperationContext.Current.OutgoingResponse.Headers[HttpResponseHeader.CacheControl] = "no-cache";
            if (allValues.ContainsKey(id))
            {
                return allValues[id];
            }
            else
            {
                throw new WebFaultException(HttpStatusCode.NotFound);
            }
        }

        public void AddValue(string value)
        {
            string id = nextId.ToString();
            nextId++;
            allValues.Add(id, value);
            string location = WebOperationContext.Current.IncomingRequest.UriTemplateMatch.RequestUri.ToString() + "/" + id;
            WebOperationContext.Current.OutgoingResponse.SetStatusAsCreated(new Uri(location));
        }

        public void DeleteValue(string id)
        {
            if (allValues.ContainsKey(id))
            {
                allValues.Remove(id);
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NoContent;
            }
            else
            {
                throw new WebFaultException(HttpStatusCode.NotFound);
            }
        }

        public string UpdateValue(string id, string value)
        {
            if (allValues.ContainsKey(id))
            {
                allValues[id] = value;
                return value;
            }
            else
            {
                throw new WebFaultException(HttpStatusCode.NotFound);
            }
        }
    }
}