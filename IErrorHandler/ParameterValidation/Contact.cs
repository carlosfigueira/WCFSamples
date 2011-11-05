using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace ParameterValidation
{
    [DataContract]
    public class Contact
    {
        [DataMember]
        [Required(ErrorMessage = "Name is required")]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "Name must have between 1 and 20 characters")]
        public string Name { get; set; }

        [DataMember]
        [Range(0, 150, ErrorMessage = "Age must be an integer between 0 and 150")]
        public int Age { get; set; }

        [DataMember]
        [Required(ErrorMessage = "E-mail is required")]
        [RegularExpression(@"[^\@]+\@[a-zA-Z0-9]+(\.[a-zA-Z0-9]+)+", ErrorMessage = "E-mail is invalid")]
        public string Email { get; set; }
    }
}
