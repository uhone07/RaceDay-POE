using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

// One category as returned by the API.
public class CategoryItem
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = "";
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal? DistanceKm { get; set; }

    public string AgeText
    {
        get
        {
            if (MinAge == null && MaxAge == null) return "All ages";
            if (MaxAge == null) return $"{MinAge}+";
            if (MinAge == null) return $"Up to {MaxAge}";
            return $"{MinAge} - {MaxAge}";
        }
    }
}

// Data for the categories list page.
public class CategoryListViewModel
{
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public List<CategoryItem> Categories { get; set; } = new();
}

// Used by the Create and Edit category forms. Matches the API's CategoryDto.
public class CategoryFormViewModel
{
    public int EventId { get; set; }
    public int CategoryId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    [Range(0, 120), Display(Name = "Minimum age")]
    public int? MinAge { get; set; }

    [Range(0, 120), Display(Name = "Maximum age")]
    public int? MaxAge { get; set; }

    [Range(0.1, 1000), Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }
}