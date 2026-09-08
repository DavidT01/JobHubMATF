using System.ComponentModel.DataAnnotations;

namespace Identity.API.Models;

public class ConfirmEmailDto
{
    [Required(ErrorMessage = "User id is required.")]
    public string? UserId { get; set; }

    [Required(ErrorMessage = "Token is required.")]
    public string? Token { get; set; }
}
