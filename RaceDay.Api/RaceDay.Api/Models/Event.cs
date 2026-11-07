using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

// One event as returned by the API.
public class EventListItem
{
    public int EventId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = "";
    public string? BannerImageUrl { get; set; }
    public int OrganiserId { get; set; }
}

// Used by the Create and Edit forms. Matches the API's EventDto.
public class EventFormViewModel
{
    public int EventId { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = "";

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required, Display(Name = "Date and time"), DataType(DataType.DateTime)]
    public DateTime EventDate { get; set; } = DateTime.Today.AddDays(7);

    [Required, MaxLength(200)]
    public string Location { get; set; } = "";

    [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km."), Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; } = 5;

    [Required, Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    public string? BannerImageUrl { get; set; }
}