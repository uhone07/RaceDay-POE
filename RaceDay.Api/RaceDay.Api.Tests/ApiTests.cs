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
    // ---------- Event management and role rejection ----------
    public class EventTests : ApiTestBase
    {
        public EventTests(RaceDayFactory f) : base(f) { }

        [Fact]
        public async Task Organiser_CanCreateEvent_Returns201()
        {
            var organiser = await LoggedInClient("Organiser");
            var res = await organiser.PostAsJsonAsync("/api/events", NewEvent());
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        }

        [Fact]
        public async Task Organiser_CanUpdateAndDeleteOwnEvent()
        {
            var organiser = await LoggedInClient("Organiser");
            var id = await CreateEvent(organiser);

            var update = await organiser.PutAsJsonAsync($"/api/events/{id}", NewEvent("Renamed Event"));
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);

            var delete = await organiser.DeleteAsync($"/api/events/{id}");
            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        }

        [Fact]
        public async Task Participant_CreatingEvent_Returns403()
        {
            var participant = await LoggedInClient("Participant");
            var res = await participant.PostAsJsonAsync("/api/events", NewEvent());
            Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        }

        [Fact]
        public async Task Unauthenticated_CreatingEvent_Returns401()
        {
            var anonymous = Factory.CreateClient();
            var res = await anonymous.PostAsJsonAsync("/api/events", NewEvent());
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        [Fact]
        public async Task Unauthenticated_ViewingEvents_Returns401()
        {
            var anonymous = Factory.CreateClient();
            var res = await anonymous.GetAsync("/api/events");
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        [Fact]
        public async Task Organiser_CannotEditAnotherOrganisersEvent_Returns403()
        {
            var owner = await LoggedInClient("Organiser");
            var other = await LoggedInClient("Organiser");
            var id = await CreateEvent(owner);

            var res = await other.PutAsJsonAsync($"/api/events/{id}", NewEvent("Hijacked"));
            Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        }

        [Fact]
        public async Task Participant_CanViewEvents_Returns200()
        {
            var participant = await LoggedInClient("Participant");
            var res = await participant.GetAsync("/api/events");
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }

        [Fact]
        public async Task Participant_ViewingEventEnrolments_Returns403()
        {
            var organiser = await LoggedInClient("Organiser");
            var participant = await LoggedInClient("Participant");
            var id = await CreateEvent(organiser);

            var res = await participant.GetAsync($"/api/events/{id}/enrolments");
            Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        }
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
// ---------- Event enrolments ----------
public class EnrolmentTests : ApiTestBase
{
    public EnrolmentTests(RaceDayFactory f) : base(f) { }

    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsRecorded()
    {
        var organiser = await LoggedInClient("Organiser");
        var participant = await LoggedInClient("Participant");
        var eventId = await CreateEvent(organiser);
        var categoryId = await CreateCategory(organiser, eventId);

        var enrol = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrol", new { categoryId });
        Assert.Equal(HttpStatusCode.Created, enrol.StatusCode);

        // The participant sees it in their own enrolments, as Pending
        var mine = await participant.GetFromJsonAsync<JsonElement>("/api/enrolments/mine");
        Assert.Equal(1, mine.GetArrayLength());
        Assert.Equal(eventId, mine[0].GetProperty("eventId").GetInt32());
        Assert.Equal("Pending", mine[0].GetProperty("status").GetString());

        // The organiser sees it on the event's enrolment list
        var all = await organiser.GetFromJsonAsync<JsonElement>($"/api/events/{eventId}/enrolments");
        Assert.Equal(1, all.GetArrayLength());
    }

    [Fact]
    public async Task Participant_EnrollingTwice_Returns409()
    {
        var organiser = await LoggedInClient("Organiser");
        var participant = await LoggedInClient("Participant");
        var eventId = await CreateEvent(organiser);
        var categoryId = await CreateCategory(organiser, eventId);

        await participant.PostAsJsonAsync($"/api/events/{eventId}/enrol", new { categoryId });
        var second = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrol", new { categoryId });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Organiser_Enrolling_Returns403()
    {
        var organiser = await LoggedInClient("Organiser");
        var eventId = await CreateEvent(organiser);
        var categoryId = await CreateCategory(organiser, eventId);

        var res = await organiser.PostAsJsonAsync($"/api/events/{eventId}/enrol", new { categoryId });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_Enrolling_Returns401()
    {
        var organiser = await LoggedInClient("Organiser");
        var eventId = await CreateEvent(organiser);
        var categoryId = await CreateCategory(organiser, eventId);

        var anonymous = Factory.CreateClient();
        var res = await anonymous.PostAsJsonAsync($"/api/events/{eventId}/enrol", new { categoryId });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Enrolling_WithCategoryFromAnotherEvent_Returns400()
    {
        var organiser = await LoggedInClient("Organiser");
        var participant = await LoggedInClient("Participant");
        var eventA = await CreateEvent(organiser);
        var eventB = await CreateEvent(organiser);
        var categoryOfB = await CreateCategory(organiser, eventB);

        var res = await participant.PostAsJsonAsync($"/api/events/{eventA}/enrol", new { categoryId = categoryOfB });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}