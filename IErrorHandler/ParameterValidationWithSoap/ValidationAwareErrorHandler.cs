using System;
using System.ComponentModel.DataAnnotations;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace ParameterValidationWithSoap
{
    public class ValidationAwareErrorHandler : IErrorHandler
    {
        public ValidationAwareErrorHandler()
        {
        }

        public bool HandleError(Exception error)
        {
            return error is ValidationException;
        }

        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {
            ValidationException validationException = error as ValidationException;
            if (validationException != null)
            {
                FaultException<string> faultException = new FaultException<string>("Error: " + validationException.ValidationResult.ErrorMessage);
                MessageFault messageFault = faultException.CreateMessageFault();
                fault = Message.CreateMessage(version, messageFault, null);
            }
        }
    }
}
