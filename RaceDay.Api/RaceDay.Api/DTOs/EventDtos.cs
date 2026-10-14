using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class EventDto
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [Required, MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    [Range(0.1, 1000)]
    public decimal DistanceKm { get; set; }

    // "Run", "Walk" or "Cycle"
    [Required]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? BannerImageUrl { get; set; }
}