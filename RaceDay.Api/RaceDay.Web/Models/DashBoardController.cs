using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireRole("Organiser")]
public class DashboardController : Controller
{
    private readonly ApiClient _api;

    public DashboardController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var all = await _api.GetAsync<List<EventListItem>>("api/events") ?? new List<EventListItem>();
        var userId = HttpContext.Session.GetInt32("UserId");

        var mine = (userId == null ? all : all.Where(e => e.OrganiserId == userId))
            .OrderBy(e => e.EventDate)
            .ToList();

        var rows = new List<DashboardEventRow>();
        foreach (var ev in mine)
        {
            var enrolments = await _api.GetAsync<List<OrganiserEnrolmentItem>>($"api/events/{ev.EventId}/enrolments")
                             ?? new List<OrganiserEnrolmentItem>();

            rows.Add(new DashboardEventRow
            {
                EventId = ev.EventId,
                Name = ev.Name,
                EventDate = ev.EventDate,
                Location = ev.Location,
                EventType = ev.EventType,
                Total = enrolments.Count(e => e.Status != "Cancelled"),
                Confirmed = enrolments.Count(e => e.Status == "Confirmed"),
                Pending = enrolments.Count(e => e.Status == "Pending")
            });
        }

        return View(new DashboardViewModel { Events = rows });
    }
}