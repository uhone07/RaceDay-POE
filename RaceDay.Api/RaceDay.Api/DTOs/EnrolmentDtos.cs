using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class EnrolDto
{
    [Required]
    public int CategoryId { get; set; }
}
public class UpdateEnrolmentStatusDto
{
    // "Pending", "Confirmed" or "Cancelled"
    [Required]
    public string Status { get; set; } = string.Empty;
}