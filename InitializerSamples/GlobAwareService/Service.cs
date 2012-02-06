using System;
using System.Net;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text.RegularExpressions;

namespace GlobAwareService
{
    [DataContract]
    public class Person
    {
        static readonly Regex emailRegex = new Regex(@"[a-z]+\@[a-z]+(\.[a-z]+)+");
        string name;
        string email;
        DateTime dateOfBirth;

        public Person(string name, string email, DateTime dateOfBirth)
        {
            this.Name = name;
            this.Email = email;
            this.DateOfBirth = dateOfBirth;
        }

        [DataMember]
        public string Name
        {
            get { return this.name; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException(StringRetriever.GetResources().GetInvalidName());
                }

                this.name = value;
            }
        }

        [DataMember]
        public string Email
        {
            get { return this.email; }
            set
            {
                if (!emailRegex.IsMatch(value))
                {
                    throw new ArgumentException(StringRetriever.GetResources().GetInvalidEMail());
                }

                this.email = value;
            }
        }

        [DataMember]
        public DateTime DateOfBirth
        {
            get { return this.dateOfBirth; }
            set
            {
                TimeSpan age = DateTime.Now.Subtract(value);
                if (age.Ticks < 0 || age.TotalDays > (151 * 365))
                {
                    throw new ArgumentException(StringRetriever.GetResources().GetInvalidDateOfBirth());
                }

                this.dateOfBirth = value;
            }
        }
    }

    [ServiceContract]
    public interface ITest
    {
        [WebGet]
        Person CreatePerson(string name, string email, string dateOfBirth);
    }
    public class Service : ITest
    {
        public Person CreatePerson(string name, string email, string dateOfBirth)
        {
            try
            {
                return new Person(name, email, DateTime.Parse(dateOfBirth));
            }
            catch (ArgumentException e)
            {
                throw new WebFaultException<string>(e.Message, HttpStatusCode.BadRequest);
            }
        }
    }
}
