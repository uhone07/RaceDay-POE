using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireRole("Participant")]
public class MyResultsController : Controller
{
    private readonly ApiClient _api;

    public MyResultsController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var results = await _api.GetAsync<List<MyResultItem>>("api/results/my") ?? new List<MyResultItem>();
        return View(results);
    }
}