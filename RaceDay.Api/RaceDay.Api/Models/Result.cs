using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Result
{
    [Key]
    public int ResultId { get; set; }

    // Foreign key to Enrolments (one result per enrolment)
    public int EnrolmentId { get; set; }

    // Maps to the SQL TIME column
    public TimeSpan FinishTime { get; set; }

    public int FinishPosition { get; set; }

    public int TotalFinishers { get; set; }

    public Enrolment? Enrolment { get; set; }
}