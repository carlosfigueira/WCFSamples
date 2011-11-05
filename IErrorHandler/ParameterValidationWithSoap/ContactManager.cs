using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace ParameterValidationWithSoap
{
    [ServiceContract]
    public interface IContactManager
    {
        [OperationContract, FaultContract(typeof(string))]
        string AddContact(Contact contact);
        [OperationContract, FaultContract(typeof(string))]
        void DeleteContact(string id);
        [OperationContract, FaultContract(typeof(string))]
        Contact GetContact(string id);
    }

    public class ContactManager : IContactManager
    {
        static Dictionary<string, Contact> contacts = new Dictionary<string, Contact>();
        static int nextId = 0;

        public string AddContact(Contact contact)
        {
            string id = (++nextId).ToString();
            contacts.Add(id, contact);
            return id;
        }

        public void DeleteContact(string id)
        {
            if (contacts.ContainsKey(id))
            {
                contacts.Remove(id);
            }
            else
            {
                throw new FaultException<string>("Contact not found");
            }
        }

        public Contact GetContact(string id)
        {
            if (contacts.ContainsKey(id))
            {
                return contacts[id];
            }
            else
            {
                throw new FaultException<string>("Contact not found");
            }
        }
    }
}
