using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Globalization;

namespace GlobAwareService
{
    class StringRetriever
    {
        public static IResourceStrings GetResources()
        {
            switch (Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName)
            {
                case "es":
                    return new SpanishStrings();
                case "pt":
                    return new PortugueseStrings();
                default:
                    return new DefaultStrings();
            }
        }
    }

    interface IResourceStrings
    {
        string GetInvalidName();
        string GetInvalidEMail();
        string GetInvalidDateOfBirth();
    }

    public class DefaultStrings : IResourceStrings
    {
        public string GetInvalidName()
        {
            return "Invalid name.";
        }

        public string GetInvalidEMail()
        {
            return "Invalid e-mail.";
        }

        public string GetInvalidDateOfBirth()
        {
            return "Invalid date of birth.";
        }
    }

    public class PortugueseStrings : IResourceStrings
    {
        public string GetInvalidName()
        {
            return "O nome é inválido.";
        }

        public string GetInvalidEMail()
        {
            return "O e-mail é inválido.";
        }

        public string GetInvalidDateOfBirth()
        {
            return "A data de nascimento é inválida.";
        }
    }

    public class SpanishStrings : IResourceStrings
    {
        public string GetInvalidName()
        {
            return "El nombre es inválido.";
        }

        public string GetInvalidEMail()
        {
            return "El correo electrónico es inválido.";
        }

        public string GetInvalidDateOfBirth()
        {
            return "La fecha de nacimiento es inválida.";
        }
    }
}
