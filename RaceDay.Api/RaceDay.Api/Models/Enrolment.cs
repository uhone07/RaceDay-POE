using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Enrolment
{
    [Key]
    public int EnrolmentId { get; set; }

    // Foreign key to Users (the participant who entered)
    public int ParticipantId { get; set; }

    // Foreign key to Events
    public int EventId { get; set; }

    // Foreign key to Categories
    public int CategoryId { get; set; }

    public DateTime EnrolmentDate { get; set; } = DateTime.Now;

    // Pending, Confirmed or Cancelled
    [Required, MaxLength(20)]
    public string Status { get; set; } = "Pending";

    public User? Participant { get; set; }
    public Event? Event { get; set; }
    public Category? Category { get; set; }
    public Result? Result { get; set; }
}