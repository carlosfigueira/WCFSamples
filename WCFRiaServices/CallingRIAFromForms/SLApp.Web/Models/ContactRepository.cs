using System.Collections.Generic;
using System.Linq;

namespace SLApp.Web.Models
{
    public class ContactRepository
    {
        static List<Contact> AllContacts;
        static int NextId = 1;
        static ContactRepository()
        {
            AllContacts = new List<Contact>();
            AllContacts.Add(new Contact
            {
                Id = NextId++,
                Name = "Scooby Doo",
                Age = 10,
                Telephone = "(415)555-1111"
            });
            AllContacts.Add(new Contact
            {
                Id = NextId++,
                Name = "Shaggy",
                Age = 22,
                Telephone = "(415)555-2222"
            });
            AllContacts.Add(new Contact
            {
                Id = NextId++,
                Name = "Fred",
                Age = 23,
                Telephone = "(415)555-3333"
            });
            AllContacts.Add(new Contact
            {
                Id = NextId++,
                Name = "Velma",
                Age = 25,
                Telephone = "(415)555-4444"
            });
            AllContacts.Add(new Contact
            {
                Id = NextId++,
                Name = "Daphne",
                Age = 21,
                Telephone = "(415)555-5555"
            });
        }

        public static IQueryable<Contact> Get()
        {
            return AllContacts.AsQueryable();
        }

        public static int Add(Contact contact)
        {
            contact.Id = NextId++;
            AllContacts.Add(contact);
            return contact.Id;
        }

        public static void Update(Contact contact)
        {
            Contact toUpdate = AllContacts.Where(x => x.Id == contact.Id).FirstOrDefault();
            if (toUpdate != null)
            {
                toUpdate.Name = contact.Name;
                toUpdate.Age = contact.Age;
                toUpdate.Telephone = contact.Telephone;
            }
        }

        public static void Delete(Contact contact)
        {
            int index = AllContacts.FindIndex(x => x.Id == contact.Id);
            if (index >= 0)
            {
                AllContacts.RemoveAt(index);
            }
        }
    }
}