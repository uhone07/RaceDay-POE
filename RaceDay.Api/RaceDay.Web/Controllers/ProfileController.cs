using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireRole] // any logged-in user
public class ProfileController : Controller
{
    private readonly ApiClient _api;

    public ProfileController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var p = await _api.GetAsync<ProfileItem>("api/profile");
        if (p == null)
        {
            TempData["Error"] = "Could not load your profile.";
            return RedirectToAction("Index", "Home");
        }

        return View(new ProfileFormViewModel
        {
            Email = p.Email,
            Role = p.Role,
            FullName = p.FullName,
            PhoneNumber = p.PhoneNumber,
            ProfilePictureUrl = p.ProfilePictureUrl
        });
    }

    [HttpPost]
    public async Task<IActionResult> Index(ProfileFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var response = await _api.SendAsync(HttpMethod.Put, "api/profile", new
        {
            model.FullName,
            model.PhoneNumber,
            model.ProfilePictureUrl
        });

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await ReadMessage(response, "Could not update your profile."));
            return View(model);
        }

        // Keep the name in the menu up to date.
        HttpContext.Session.SetString("FullName", model.FullName);

        TempData["Success"] = "Profile updated.";
        return RedirectToAction("Index");
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