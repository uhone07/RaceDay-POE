using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

// Profile as returned by GET api/profile.
public class ProfileItem
{
    public int UserId { get; set; }
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

// Used by the profile form. Matches the API's UpdateProfileDto.
public class ProfileFormViewModel
{
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";

    [Required, Display(Name = "Full name"), MaxLength(150)]
    public string FullName { get; set; } = "";

    [Display(Name = "Phone number"), MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public string? ProfilePictureUrl { get; set; }
}
