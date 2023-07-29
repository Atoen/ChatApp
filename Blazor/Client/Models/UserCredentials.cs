using System.ComponentModel.DataAnnotations;

namespace Blazor.Client.Models;

public class UserCredentials
{
    [Required]
    [MinLength(3, ErrorMessage = "Username is too short")]
    [MaxLength(16, ErrorMessage = "Username is too long")]
    public string? Username { get; set; }

    [Required]
    [MinLength(3, ErrorMessage = "Password is too short")]
    public string? Password { get; set; }
}