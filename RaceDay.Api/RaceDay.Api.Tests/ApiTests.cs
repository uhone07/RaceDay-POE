using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Api.Data;
using Xunit;

namespace RaceDay.Api.Tests;

// Runs the real API in memory with a fake database, so no SQL Server is needed.
public class RaceDayFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<RaceDayDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<RaceDayDbContext>>();
            services.AddDbContext<RaceDayDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }
}

public abstract class ApiTestBase : IClassFixture<RaceDayFactory>
{
    protected readonly RaceDayFactory Factory;

    protected ApiTestBase(RaceDayFactory factory) => Factory = factory;

    // Registers a new user and logs in. Each client keeps its own session cookie.
    protected async Task<HttpClient> LoggedInClient(string role)
    {
        var client = Factory.CreateClient();
        var email = $"jarred-{Guid.NewGuid():N}@test.com";

        var reg = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Password123",
            fullName = "Jarred Tester",
            phoneNumber = "0821234567",
            role
        });
        reg.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123" });
        login.EnsureSuccessStatusCode();
        return client;
    }

    protected static object NewEvent(string name = "Jarred's Test Run") => new
    {
        name,
        description = "Test event",
        eventDate = DateTime.UtcNow.AddDays(30),
        location = "Johannesburg",
        distanceKm = 10,
        eventType = "Run"
    };

    protected static async Task<int> ReadId(HttpResponseMessage response, string property)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty(property).GetInt32();
    }

    protected async Task<int> CreateEvent(HttpClient organiser)
    {
        var res = await organiser.PostAsJsonAsync("/api/events", NewEvent());
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return await ReadId(res, "eventId");
    }

    protected async Task<int> CreateCategory(HttpClient organiser, int eventId)
    {
        var res = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { name = "Senior", minAge = 20, maxAge = 59, distanceKm = 10 });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return await ReadId(res, "categoryId");
    }
}

// ---------- Registration and login ----------
public class AuthTests : ApiTestBase
{
    public AuthTests(RaceDayFactory f) : base(f) { }

    [Fact]
    public async Task Register_ValidParticipant_Returns201()
    {
        var client = Factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"jarred-{Guid.NewGuid():N}@test.com",
            password = "Password123",
            fullName = "Jarred",
            role = "Participant"
        });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = Factory.CreateClient();
        var body = new { email = $"dup-{Guid.NewGuid():N}@test.com", password = "Password123", fullName = "Jarred", role = "Participant" };
        await client.PostAsJsonAsync("/api/auth/register", body);
        var res = await client.PostAsJsonAsync("/api/auth/register", body);
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var client = Factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"bad-{Guid.NewGuid():N}@test.com",
            password = "Password123",
            fullName = "Jarred",
            role = "Admin"
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Login_CorrectCredentials_Returns200()
    {
        var client = Factory.CreateClient();
        var email = $"login-{Guid.NewGuid():N}@test.com";
        await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Password123", fullName = "Jarred", role = "Participant" });

        var res = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123" });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = Factory.CreateClient();
        var email = $"wrong-{Guid.NewGuid():N}@test.com";
        await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Password123", fullName = "Jarred", role = "Participant" });

        var res = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_AfterLogout_Returns401()
    {
        var client = await LoggedInClient("Participant");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/profile")).StatusCode);

        await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/profile")).StatusCode);
    }
}