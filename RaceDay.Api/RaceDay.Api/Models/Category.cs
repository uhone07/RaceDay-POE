using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Category
{
    [Key]
    public int CategoryId { get; set; }

    // Foreign key to Events
    public int EventId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal? DistanceKm { get; set; }

    public Event? Event { get; set; }
}