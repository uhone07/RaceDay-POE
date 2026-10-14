using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class ResultDto
{
    // Format "hh:mm:ss", e.g. "01:45:32"
    [Required]
    public TimeSpan FinishTime { get; set; }

    [Range(1, int.MaxValue)]
    public int FinishPosition { get; set; }

    [Range(1, int.MaxValue)]
    public int TotalFinishers { get; set; }
}