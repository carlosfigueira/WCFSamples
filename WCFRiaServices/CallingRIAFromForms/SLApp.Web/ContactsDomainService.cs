namespace SLApp.Web
{
    using System.Linq;
    using System.Net;
    using System.ServiceModel;
    using System.ServiceModel.DomainServices.Hosting;
    using System.ServiceModel.DomainServices.Server;
    using System.ServiceModel.Web;
    using SLApp.Web.Models;

    [EnableClientAccess()]
    public class ContactsDomainService : DomainService
    {
        [Query]
        public IQueryable<Contact> GetAllContacts()
        {
            return ContactRepository.Get();
        }

        [Insert]
        public void CreateContact(Contact contact)
        {
            ContactRepository.Add(contact);
        }

        [Update]
        public void UpdateContact(Contact contact)
        {
            ContactRepository.Update(contact);
        }

        [Delete]
        public void RemoveContact(Contact contact)
        {
            ContactRepository.Delete(contact);
        }

        [Invoke(HasSideEffects = true)]
        [CanReceiveFormsUrlEncodedInput]
        public Contact ImportContact(Contact contact)
        {
            ContactRepository.Add(contact);
            object fromFormsUrlEncoded;
            if (OperationContext.Current.IncomingMessageProperties.TryGetValue(FormUrlEncodedEnabledJsonEndpointFactory.FormUrlEncodedInputProperty, out fromFormsUrlEncoded))
            {
                if ((bool)fromFormsUrlEncoded)
                {
                    WebOperationContext.Current.OutgoingResponse.SuppressEntityBody = true;
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Redirect;
                    // Start page is two levels up: domainservice.svc/JSON
                    WebOperationContext.Current.OutgoingResponse.Headers[HttpResponseHeader.Location] = "../../SLAppTestPage.html";
                }
            }

            return contact;
        }
    }
}
