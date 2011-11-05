using System.Collections.Generic;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace ParameterValidation
{
    [ServiceContract]
    public class ContactManager
    {
        static Dictionary<string, Contact> contacts = new Dictionary<string, Contact>();
        static int nextId = 0;

        [WebInvoke(Method = "POST", UriTemplate = "/Contact", ResponseFormat = WebMessageFormat.Json)]
        public string AddContact(Contact contact)
        {
            string id = (++nextId).ToString();
            contacts.Add(id, contact);
            string requestUri = WebOperationContext.Current.IncomingRequest.UriTemplateMatch.RequestUri.ToString();
            if (requestUri.EndsWith("/"))
            {
                requestUri = requestUri.Substring(0, requestUri.Length - 1);
            }

            WebOperationContext.Current.OutgoingResponse.Headers[HttpResponseHeader.Location] = requestUri + "s/" + id;
            return id;
        }

        [WebInvoke(Method = "DELETE", UriTemplate = "/Contact/{id}")]
        public void DeleteContact(string id)
        {
            if (contacts.ContainsKey(id))
            {
                contacts.Remove(id);
            }
            else
            {
                throw new WebFaultException(HttpStatusCode.NotFound);
            }
        }

        [WebGet(UriTemplate = "/Contacts/{id}", ResponseFormat = WebMessageFormat.Json)]
        public Contact GetContact(string id)
        {
            if (contacts.ContainsKey(id))
            {
                return contacts[id];
            }
            else
            {
                throw new WebFaultException(HttpStatusCode.NotFound);
            }
        }
    }
}
