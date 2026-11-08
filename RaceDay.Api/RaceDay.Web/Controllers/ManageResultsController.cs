using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireRole("Organiser")]
public class ManageResultsController : Controller
{
    private readonly ApiClient _api;

    public ManageResultsController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Create(int eventId, int enrolmentId)
    {
        var enrolments = await _api.GetAsync<List<OrganiserEnrolmentItem>>($"api/events/{eventId}/enrolments")
                         ?? new List<OrganiserEnrolmentItem>();
        var e = enrolments.FirstOrDefault(x => x.EnrolmentId == enrolmentId);

        if (e == null)
        {
            TempData["Error"] = "Enrolment not found.";
            return RedirectToAction("Index", "ManageEnrolments", new { eventId });
        }

        return View(new ResultFormViewModel
        {
            EventId = eventId,
            EnrolmentId = enrolmentId,
            ParticipantName = e.ParticipantName,
            FinishPosition = 1,
            TotalFinishers = Math.Max(1, enrolments.Count(x => x.Status != "Cancelled"))
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(int eventId, int enrolmentId, ResultFormViewModel model)
    {
        model.EventId = eventId;
        model.EnrolmentId = enrolmentId;

        var dto = BuildDto(model);
        if (dto == null) return View(model);

        var response = await _api.SendAsync(HttpMethod.Post, $"api/enrolments/{enrolmentId}/results", dto);
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await ReadMessage(response, "Could not save the result."));
            return View(model);
        }

        TempData["Success"] = "Result saved.";
        return RedirectToAction("Index", "ManageEnrolments", new { eventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int eventId, int id)
    {
        var results = await _api.GetAsync<List<OrganiserResultItem>>($"api/events/{eventId}/results")
                      ?? new List<OrganiserResultItem>();
        var r = results.FirstOrDefault(x => x.ResultId == id);

        if (r == null)
        {
            TempData["Error"] = "Result not found.";
            return RedirectToAction("Index", "ManageEnrolments", new { eventId });
        }

        return View(new ResultFormViewModel
        {
            EventId = eventId,
            EnrolmentId = r.EnrolmentId,
            ResultId = id,
            ParticipantName = r.ParticipantName,
            FinishTime = r.FinishTime.ToString("c"),
            FinishPosition = r.FinishPosition,
            TotalFinishers = r.TotalFinishers
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int eventId, int id, ResultFormViewModel model)
    {
        model.EventId = eventId;
        model.ResultId = id;

        var dto = BuildDto(model);
        if (dto == null) return View(model);

        var response = await _api.SendAsync(HttpMethod.Put, $"api/results/{id}", dto);
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await ReadMessage(response, "Could not update the result."));
            return View(model);
        }

        TempData["Success"] = "Result updated.";
        return RedirectToAction("Index", "ManageEnrolments", new { eventId });
    }

    // Checks the form and builds the object the API expects. Returns null if something is wrong.
    private object? BuildDto(ResultFormViewModel m)
    {
        if (!ModelState.IsValid) return null;

        if (!TimeSpan.TryParse(m.FinishTime, out var time))
        {
            ModelState.AddModelError("FinishTime", "Enter the time as hh:mm:ss, for example 01:45:32.");
            return null;
        }

        if (m.FinishPosition > m.TotalFinishers)
        {
            ModelState.AddModelError("", "Finishing position cannot be greater than the total number of finishers.");
            return null;
        }

        return new
        {
            FinishTime = time,
            m.FinishPosition,
            m.TotalFinishers
        };
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