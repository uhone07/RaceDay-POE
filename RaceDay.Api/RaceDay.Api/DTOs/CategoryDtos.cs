using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class CategoryDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 120)]
    public int? MinAge { get; set; }

    [Range(0, 120)]
    public int? MaxAge { get; set; }

    [Range(0.1, 1000)]
    public decimal? DistanceKm { get; set; }
}