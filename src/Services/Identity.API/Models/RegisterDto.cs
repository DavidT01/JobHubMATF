using System.ComponentModel.DataAnnotations;

namespace Identity.API.Models
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Ime je obavezno.")]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Prezime je obavezno.")]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "Email je obavezan.")]
        [EmailAddress(ErrorMessage = "Nevalidan format email adrese.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Lozinka je obavezna.")]
        [MinLength(6, ErrorMessage = "Lozinka mora imati barem 6 karaktera.")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Uloga je obavezna.")]
        public string? Role { get; set; } 
    }
}