using System.ComponentModel.DataAnnotations;
using System.ServiceModel.Dispatcher;

namespace ParameterValidationWithSoap
{
    public class ValidatingParameterInspector : IParameterInspector
    {
        public void AfterCall(string operationName, object[] outputs, object returnValue, object correlationState)
        {
        }

        public object BeforeCall(string operationName, object[] inputs)
        {
            foreach (var input in inputs)
            {
                if (input != null)
                {
                    ValidationContext context = new ValidationContext(input, null, null);
                    Validator.ValidateObject(input, context, true);
                }
            }

            return null;
        }
    }
}
