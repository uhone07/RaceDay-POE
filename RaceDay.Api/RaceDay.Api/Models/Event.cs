using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Event
{
    [Key]
    public int EventId { get; set; }

    // Foreign key to Users (the organiser who created the event)
    public int OrganiserId { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public DateTime EventDate { get; set; }

    [Required, MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    public decimal DistanceKm { get; set; }

    // Run, Walk or Cycle
    [Required, MaxLength(10)]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? BannerImageUrl { get; set; }

    public User? Organiser { get; set; }
    public List<Category> Categories { get; set; } = new();
    public List<Enrolment> Enrolments { get; set; } = new();
}
