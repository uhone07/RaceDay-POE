using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class RegisterViewModel
{
    [Required, Display(Name = "Full name"), MaxLength(150)]
    public string FullName { get; set; } = "";

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = "";

    [Display(Name = "Phone number"), MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [Required, MinLength(8, ErrorMessage = "Password must be at least 8 characters."), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required]
    public string Role { get; set; } = "Participant";
}

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";
}
