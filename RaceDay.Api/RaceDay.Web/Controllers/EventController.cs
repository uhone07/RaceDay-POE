using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireRole("Participant")]
public class EventsController : Controller
{
    private readonly ApiClient _api;

    public EventsController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(string? type, string? search, decimal? maxDistance, bool includePast = false)
    {
        var url = string.IsNullOrWhiteSpace(type) ? "api/events" : $"api/events?type={Uri.EscapeDataString(type)}";
        var all = await _api.GetAsync<List<EventListItem>>(url) ?? new List<EventListItem>();

        IEnumerable<EventListItem> events = all;

        if (!includePast)
            events = events.Where(e => e.EventDate >= DateTime.Now);

        if (!string.IsNullOrWhiteSpace(search))
            events = events.Where(e =>
                e.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.Location.Contains(search, StringComparison.OrdinalIgnoreCase));

        if (maxDistance.HasValue)
            events = events.Where(e => e.DistanceKm <= maxDistance.Value);

        return View(new EventsListViewModel
        {
            Events = events.OrderBy(e => e.EventDate).ToList(),
            Type = type,
            Search = search,
            MaxDistance = maxDistance,
            IncludePast = includePast
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var ev = await _api.GetAsync<EventDetailsItem>($"api/events/{id}");
        if (ev == null)
        {
            TempData["Error"] = "Event not found.";
            return RedirectToAction("Index");
        }

        var mine = await _api.GetAsync<List<MyEnrolmentItem>>("api/enrolments/my") ?? new List<MyEnrolmentItem>();

        return View(new EventDetailsViewModel
        {
            Event = ev,
            Enrolment = mine.FirstOrDefault(m => m.EventId == id)
        });
    }

    [HttpPost]
    public async Task<IActionResult> Enrol(int id, int categoryId)
    {
        if (categoryId <= 0)
        {
            TempData["Error"] = "Please choose a category.";
            return RedirectToAction("Details", new { id });
        }

        var response = await _api.SendAsync(HttpMethod.Post, $"api/events/{id}/enrolments", new { CategoryId = categoryId });

        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "You are enrolled. Your enrolment is pending until the organiser confirms it.";
            return RedirectToAction("Index", "MyEnrolments");
        }

        TempData["Error"] = await ReadMessage(response, "Could not enrol you in this event.");
        return RedirectToAction("Details", new { id });
    }

    private static async Task<string> ReadMessage(HttpResponseMessage response, string fallback)
    {
        try
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (json.ValueKind == JsonValueKind.Object
                && json.TryGetProperty("message", out var msg)
                && msg.ValueKind == JsonValueKind.String)
                return msg.GetString() ?? fallback;
        }
        catch { }
        return fallback;
    }
}