namespace RaceDay.Web.Models;

// One event with its categories, as returned by GET api/events/{id}.
public class EventDetailsItem
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
    public List<CategoryItem> Categories { get; set; } = new();
}

public class EventsListViewModel
{
    public List<EventListItem> Events { get; set; } = new();
    public string? Type { get; set; }
    public string? Search { get; set; }
    public decimal? MaxDistance { get; set; }
    public bool IncludePast { get; set; }
}

public class EventDetailsViewModel
{
    public EventDetailsItem Event { get; set; } = new();
    public MyEnrolmentItem? Enrolment { get; set; }
}

// One of the logged-in Participant's own enrolments.
public class MyEnrolmentItem
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime EnrolmentDate { get; set; }

    public string BadgeClass => Status switch
    {
        "Confirmed" => "bg-success",
        "Pending" => "bg-warning text-dark",
        "Cancelled" => "bg-danger",
        _ => "bg-secondary"
    };
}

// One of the logged-in Participant's own results.
public class MyResultItem
{
    public int ResultId { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = "";
    public TimeSpan FinishTime { get; set; }
    public int FinishPosition { get; set; }
    public int TotalFinishers { get; set; }

    // e.g. "47th out of 312"
    public string PositionText => $"{FinishPosition}{Suffix(FinishPosition)} out of {TotalFinishers}";

    private static string Suffix(int n)
    {
        var last2 = n % 100;
        if (last2 is >= 11 and <= 13) return "th";
        return (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
    }
}