using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireRole("Participant")]
public class MyEnrolmentsController : Controller
{
    private readonly ApiClient _api;

    public MyEnrolmentsController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var mine = await _api.GetAsync<List<MyEnrolmentItem>>("api/enrolments/my") ?? new List<MyEnrolmentItem>();
        return View(mine);
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var response = await _api.SendAsync(HttpMethod.Put, $"api/enrolments/{id}/cancel");

        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Enrolment cancelled.";
        }
        else
        {
            var message = "Could not cancel the enrolment.";
            try
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (json.ValueKind == JsonValueKind.Object
                    && json.TryGetProperty("message", out var msg)
                    && msg.ValueKind == JsonValueKind.String)
                    message = msg.GetString() ?? message;
            }
            catch { }
            TempData["Error"] = message;
        }

        return RedirectToAction("Index");
    }
}