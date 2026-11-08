using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireRole("Organiser")]
public class ManageEventsController : Controller
{
    private readonly ApiClient _api;

    public ManageEventsController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var all = await _api.GetAsync<List<EventListItem>>("api/events") ?? new List<EventListItem>();
        var userId = HttpContext.Session.GetInt32("UserId");
        var mine = userId == null ? all : all.Where(e => e.OrganiserId == userId).ToList();
        return View(mine);
    }

    [HttpGet]
    public IActionResult Create() => View(new EventFormViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (!await UploadBannerIfChosen(model)) return View(model);

        var response = await _api.SendAsync(HttpMethod.Post, "api/events", ToDto(model));
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await ReadMessage(response, "Could not create the event."));
            return View(model);
        }

        TempData["Success"] = "Event created.";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var ev = await _api.GetAsync<EventListItem>($"api/events/{id}");
        if (ev == null)
        {
            TempData["Error"] = "Event not found.";
            return RedirectToAction("Index");
        }

        return View(new EventFormViewModel
        {
            EventId = ev.EventId,
            Name = ev.Name,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            DistanceKm = ev.DistanceKm,
            EventType = ev.EventType,
            BannerImageUrl = ev.BannerImageUrl
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        model.EventId = id;
        if (!ModelState.IsValid) return View(model);

        // Keep the current banner unless a new picture was chosen.
        if (model.BannerFile == null || model.BannerFile.Length == 0)
        {
            var existing = await _api.GetAsync<EventListItem>($"api/events/{id}");
            model.BannerImageUrl = existing?.BannerImageUrl;
        }
        else if (!await UploadBannerIfChosen(model))
        {
            return View(model);
        }

        var response = await _api.SendAsync(HttpMethod.Put, $"api/events/{id}", ToDto(model));
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await ReadMessage(response, "Could not update the event."));
            return View(model);
        }

        TempData["Success"] = "Event updated.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _api.SendAsync(HttpMethod.Delete, $"api/events/{id}");

        if (response.IsSuccessStatusCode)
            TempData["Success"] = "Event deleted.";
        else
            TempData["Error"] = await ReadMessage(response, "Could not delete the event.");

        return RedirectToAction("Index");
    }

    // If the organiser chose a picture, send it to the API (which stores it in Azure Blob Storage)
    // and keep the returned URL. Returns false (with an error on the form) if the upload fails.
    private async Task<bool> UploadBannerIfChosen(EventFormViewModel model)
    {
        if (model.BannerFile == null || model.BannerFile.Length == 0)
            return true;

        var (url, error) = await _api.UploadImageAsync("api/uploads/event-banner", model.BannerFile);
        if (url == null)
        {
            ModelState.AddModelError(nameof(model.BannerFile), error ?? "The image could not be uploaded.");
            return false;
        }

        model.BannerImageUrl = url;
        return true;
    }

    private static object ToDto(EventFormViewModel m) => new
    {
        m.Name,
        m.Description,
        m.EventDate,
        m.Location,
        m.DistanceKm,
        m.EventType,
        m.BannerImageUrl
    };

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