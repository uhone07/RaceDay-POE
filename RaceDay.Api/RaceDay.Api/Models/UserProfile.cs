using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class UserProfile
{
    [Key]
    public int ProfileId { get; set; }

    // Foreign key to Users
    public int UserId { get; set; }

    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(500)]
    public string? ProfilePictureUrl { get; set; }

    public User? User { get; set; }
}