using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireRole("Organiser")]
public class ManageEnrolmentsController : Controller
{
    private readonly ApiClient _api;

    public ManageEnrolmentsController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(int eventId)
    {
        var ev = await _api.GetAsync<EventListItem>($"api/events/{eventId}");
        if (ev == null)
        {
            TempData["Error"] = "Event not found.";
            return RedirectToAction("Index", "ManageEvents");
        }

        var response = await _api.SendAsync(HttpMethod.Get, $"api/events/{eventId}/enrolments");
        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = await ReadMessage(response, "Could not load the enrolments.");
            return RedirectToAction("Index", "ManageEvents");
        }

        var enrolments = await response.Content.ReadFromJsonAsync<List<OrganiserEnrolmentItem>>()
                         ?? new List<OrganiserEnrolmentItem>();
        var results = await _api.GetAsync<List<OrganiserResultItem>>($"api/events/{eventId}/results")
                      ?? new List<OrganiserResultItem>();

        var rows = enrolments.Select(e => new EnrolmentRow
        {
            Enrolment = e,
            Result = results.FirstOrDefault(r => r.EnrolmentId == e.EnrolmentId)
        }).ToList();

        return View(new EnrolmentListViewModel
        {
            EventId = eventId,
            EventName = ev.Name,
            Rows = rows
        });
    }

    [HttpPost]
    public async Task<IActionResult> SetStatus(int eventId, int id, string status)
    {
        var response = await _api.SendAsync(HttpMethod.Put, $"api/enrolments/{id}/status", new { Status = status });

        if (response.IsSuccessStatusCode)
            TempData["Success"] = "Status updated.";
        else
            TempData["Error"] = await ReadMessage(response, "Could not update the status.");

        return RedirectToAction("Index", new { eventId });
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