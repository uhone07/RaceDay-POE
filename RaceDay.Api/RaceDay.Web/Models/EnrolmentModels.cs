using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

// One enrolment as the Organiser sees it.
public class OrganiserEnrolmentItem
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime EnrolmentDate { get; set; }

    // Colour coding for the status badge.
    public string BadgeClass => Status switch
    {
        "Confirmed" => "bg-success",
        "Pending" => "bg-warning text-dark",
        "Cancelled" => "bg-danger",
        _ => "bg-secondary"
    };
}

// One captured result.
public class OrganiserResultItem
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public string ParticipantName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public TimeSpan FinishTime { get; set; }
    public int FinishPosition { get; set; }
    public int TotalFinishers { get; set; }
}

public class EnrolmentRow
{
    public OrganiserEnrolmentItem Enrolment { get; set; } = new();
    public OrganiserResultItem? Result { get; set; }
}

public class EnrolmentListViewModel
{
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public List<EnrolmentRow> Rows { get; set; } = new();
}

// Used by the Capture and Edit result forms. Matches the API's ResultDto.
public class ResultFormViewModel
{
    public int EventId { get; set; }
    public int EnrolmentId { get; set; }
    public int ResultId { get; set; }
    public string ParticipantName { get; set; } = "";

    [Required, Display(Name = "Finish time (hh:mm:ss)")]
    [RegularExpression(@"^\d{1,2}:[0-5]\d:[0-5]\d$", ErrorMessage = "Enter the time as hh:mm:ss, for example 01:45:32.")]
    public string FinishTime { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Position must be 1 or more."), Display(Name = "Finishing position")]
    public int FinishPosition { get; set; } = 1;

    [Range(1, int.MaxValue, ErrorMessage = "Total finishers must be 1 or more."), Display(Name = "Total finishers")]
    public int TotalFinishers { get; set; } = 1;
}
