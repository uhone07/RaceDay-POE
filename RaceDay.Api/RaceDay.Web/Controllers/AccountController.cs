using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : Controller
{
    private readonly ApiClient _api;

    public AccountController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    public IActionResult AccessDenied() => View();

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var response = await _api.SendAsync(HttpMethod.Post, "api/auth/register", new
        {
            model.Email,
            model.Password,
            model.FullName,
            model.PhoneNumber,
            model.Role
        });

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await ReadMessage(response, "Registration failed. Please check your details."));
            return View(model);
        }

        TempData["Success"] = "Account created. Please log in.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var response = await _api.SendAsync(HttpMethod.Post, "api/auth/login", new
        {
            model.Email,
            model.Password
        });

        if (!response.IsSuccessStatusCode)
        {
            _api.ForgetSession();
            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var role = FindString(json, "role");
        if (role == null)
        {
            _api.ForgetSession();
            ModelState.AddModelError("", "Login worked but the API did not return a role.");
            return View(model);
        }

        HttpContext.Session.SetString("Role", role);
        HttpContext.Session.SetString("FullName", FindString(json, "fullName") ?? model.Email);

        var userId = FindNumber(json, "userId") ?? FindNumber(json, "id");
        if (userId != null) HttpContext.Session.SetInt32("UserId", userId.Value);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        try { await _api.SendAsync(HttpMethod.Post, "api/auth/logout"); } catch { }
        _api.ForgetSession();
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }

    private static int? FindNumber(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;
        foreach (var prop in element.EnumerateObject())
        {
            if (prop.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                && prop.Value.ValueKind == JsonValueKind.Number)
                return prop.Value.GetInt32();

            if (prop.Value.ValueKind == JsonValueKind.Object)
            {
                var nested = FindNumber(prop.Value, name);
                if (nested != null) return nested;
            }
        }
        return null;
    }

    private static string? FindString(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;
        foreach (var prop in element.EnumerateObject())
        {
            if (prop.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                && prop.Value.ValueKind == JsonValueKind.String)
                return prop.Value.GetString();

            if (prop.Value.ValueKind == JsonValueKind.Object)
            {
                var nested = FindString(prop.Value, name);
                if (nested != null) return nested;
            }
        }
        return null;
    }

    private static async Task<string> ReadMessage(HttpResponseMessage response, string fallback)
    {
        try
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return FindString(json, "message") ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }
}