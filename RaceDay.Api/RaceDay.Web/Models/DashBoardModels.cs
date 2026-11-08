namespace RaceDay.Web.Models;

public class DashboardEventRow
{
    public int EventId { get; set; }
    public string Name { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = "";
    public string EventType { get; set; } = "";
    public int Total { get; set; }
    public int Confirmed { get; set; }
    public int Pending { get; set; }

    public bool IsUpcoming => EventDate >= DateTime.Now;
}

public class DashboardViewModel
{
    public List<DashboardEventRow> Events { get; set; } = new();

    public int TotalEvents => Events.Count;
    public int UpcomingEvents => Events.Count(e => e.IsUpcoming);
    public int TotalEnrolments => Events.Sum(e => e.Total);
    public int PendingEnrolments => Events.Sum(e => e.Pending);
}