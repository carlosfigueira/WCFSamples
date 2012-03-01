using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace SLApp.Web
{
    public class RestService : IRestService
    {
        #region IRestService Members

        public int AddGetJson(int x, int y)
        {
            return x + y;
        }

        public int AddGetXml(int x, int y)
        {
            return x + y;
        }

        public int SubtractPostJson(int x, int y)
        {
            return x - y;
        }

        public int SubtractPostXml(int x, int y)
        {
            return x - y;
        }

        string Reverse(string text)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = text.Length - 1; i >= 0; i--)
            {
                sb.Append(text[i]);
            }
            return sb.ToString();
        }

        public string ReverseUriTemplateGet(string text)
        {
            return Reverse(text);
        }

        public Person CreatePerson(string name, int age, DateTime dateOfBirth)
        {
            Person result = new Person();
            result.Name = name;
            result.Age = age;
            result.DOB = dateOfBirth;
            return result;
        }

        public int CreateOrder(Order order)
        {
            int orderId = order.Items.Count;
            return orderId;
        }

        #endregion
    }
}
