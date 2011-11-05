using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Xml;

namespace ParameterValidation
{
    public class ValidationAwareErrorHandler : IErrorHandler
    {
        IErrorHandler originalErrorHandler;
        public ValidationAwareErrorHandler(IErrorHandler originalErrorHandler)
        {
            this.originalErrorHandler = originalErrorHandler;
        }

        public bool HandleError(Exception error)
        {
            return error is ValidationException || this.originalErrorHandler.HandleError(error);
        }

        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {
            ValidationException validationException = error as ValidationException;
            if (validationException != null)
            {
                fault = Message.CreateMessage(version, null, new ValidationErrorBodyWriter(validationException));
                HttpResponseMessageProperty prop = new HttpResponseMessageProperty();
                prop.StatusCode = HttpStatusCode.BadRequest;
                prop.Headers[HttpResponseHeader.ContentType] = "application/json; charset=utf-8";
                fault.Properties.Add(HttpResponseMessageProperty.Name, prop);
                fault.Properties.Add(WebBodyFormatMessageProperty.Name, new WebBodyFormatMessageProperty(WebContentFormat.Json));
            }
            else
            {
                this.originalErrorHandler.ProvideFault(error, version, ref fault);
            }
        }

        class ValidationErrorBodyWriter : BodyWriter
        {
            private ValidationException validationException;
            Encoding utf8Encoding = new UTF8Encoding(false);

            public ValidationErrorBodyWriter(ValidationException validationException)
                : base(true)
            {
                this.validationException = validationException;
            }

            protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
            {
                writer.WriteStartElement("root");
                writer.WriteAttributeString("type", "object");

                writer.WriteStartElement("ErrorMessage");
                writer.WriteAttributeString("type", "string");
                writer.WriteString(this.validationException.ValidationResult.ErrorMessage);
                writer.WriteEndElement();

                writer.WriteStartElement("MemberNames");
                writer.WriteAttributeString("type", "array");
                foreach (var member in this.validationException.ValidationResult.MemberNames)
                {
                    writer.WriteStartElement("item");
                    writer.WriteAttributeString("type", "string");
                    writer.WriteString(member);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();

                writer.WriteEndElement();
            }
        }
    }
}
