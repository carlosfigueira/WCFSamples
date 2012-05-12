using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.ServiceModel.Web;

namespace CorsEnabledService
{
    [ServiceContract]
    public interface IValues
    {
        [WebGet(UriTemplate = "values", ResponseFormat = WebMessageFormat.Json), CorsEnabled]
        List<string> GetValues();
        [WebGet(UriTemplate = "values/{id}", ResponseFormat = WebMessageFormat.Json), CorsEnabled]
        string GetValue(string id);
        [WebInvoke(UriTemplate = "/values", Method = "POST", ResponseFormat = WebMessageFormat.Json), CorsEnabled]
        void AddValue(string value);
        [WebInvoke(UriTemplate = "/values/{id}", Method = "DELETE", ResponseFormat = WebMessageFormat.Json), CorsEnabled]
        void DeleteValue(string id);
        [WebInvoke(UriTemplate = "/values/{id}", Method = "PUT", ResponseFormat = WebMessageFormat.Json), CorsEnabled]
        string UpdateValue(string id, string value);
    }
}
