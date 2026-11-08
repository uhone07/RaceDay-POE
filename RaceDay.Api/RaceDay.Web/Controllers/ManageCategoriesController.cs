using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireRole("Organiser")]
public class ManageCategoriesController : Controller
{
    private readonly ApiClient _api;

    public ManageCategoriesController(ApiClient api)
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

        var categories = await _api.GetAsync<List<CategoryItem>>($"api/events/{eventId}/categories")
                         ?? new List<CategoryItem>();

        return View(new CategoryListViewModel
        {
            EventId = eventId,
            EventName = ev.Name,
            Categories = categories
        });
    }

    [HttpGet]
    public IActionResult Create(int eventId) => View(new CategoryFormViewModel { EventId = eventId });

    [HttpPost]
    public async Task<IActionResult> Create(int eventId, CategoryFormViewModel model)
    {
        model.EventId = eventId;
        if (!IsValid(model)) return View(model);

        var response = await _api.SendAsync(HttpMethod.Post, $"api/events/{eventId}/categories", ToDto(model));
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await ReadMessage(response, "Could not add the category."));
            return View(model);
        }

        TempData["Success"] = "Category added.";
        return RedirectToAction("Index", new { eventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int eventId, int id)
    {
        var list = await _api.GetAsync<List<CategoryItem>>($"api/events/{eventId}/categories")
                   ?? new List<CategoryItem>();
        var c = list.FirstOrDefault(x => x.CategoryId == id);

        if (c == null)
        {
            TempData["Error"] = "Category not found.";
            return RedirectToAction("Index", new { eventId });
        }

        return View(new CategoryFormViewModel
        {
            EventId = eventId,
            CategoryId = id,
            Name = c.Name,
            MinAge = c.MinAge,
            MaxAge = c.MaxAge,
            DistanceKm = c.DistanceKm
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int eventId, int id, CategoryFormViewModel model)
    {
        model.EventId = eventId;
        model.CategoryId = id;
        if (!IsValid(model)) return View(model);

        var response = await _api.SendAsync(HttpMethod.Put, $"api/categories/{id}", ToDto(model));
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await ReadMessage(response, "Could not update the category."));
            return View(model);
        }

        TempData["Success"] = "Category updated.";
        return RedirectToAction("Index", new { eventId });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int eventId, int id)
    {
        var response = await _api.SendAsync(HttpMethod.Delete, $"api/categories/{id}");

        if (response.IsSuccessStatusCode)
            TempData["Success"] = "Category deleted.";
        else
            TempData["Error"] = await ReadMessage(response, "Could not delete the category.");

        return RedirectToAction("Index", new { eventId });
    }

    private bool IsValid(CategoryFormViewModel m)
    {
        if (m.MinAge.HasValue && m.MaxAge.HasValue && m.MinAge > m.MaxAge)
            ModelState.AddModelError("", "Minimum age cannot be greater than maximum age.");
        return ModelState.IsValid;
    }

    private static object ToDto(CategoryFormViewModel m) => new
    {
        m.Name,
        m.MinAge,
        m.MaxAge,
        m.DistanceKm
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