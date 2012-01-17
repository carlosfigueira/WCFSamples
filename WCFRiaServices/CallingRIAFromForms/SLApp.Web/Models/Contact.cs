using System.ComponentModel.DataAnnotations;

namespace SLApp.Web.Models
{
    public class Contact
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public int Age { get; set; }

        public string Telephone { get; set; }
    }
}