using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class User
{
    [Key]
    public int UserId { get; set; }

    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Role { get; set; } = "Participant";

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // One user has one profile
    public UserProfile? Profile { get; set; }
}
