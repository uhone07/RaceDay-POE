using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class EnrolDto
{
    [Required]
    public int CategoryId { get; set; }
}