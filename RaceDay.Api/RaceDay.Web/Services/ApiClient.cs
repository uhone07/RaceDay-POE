using System.Net.Http.Json;

namespace RaceDay.Web.Services;

public class ApiClient
{
    private const string CookieKey = "ApiSessionCookie";
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;

    public ApiClient(HttpClient http, IHttpContextAccessor accessor)
    {
        _http = http;
        _accessor = accessor;
    }

    private ISession Session => _accessor.HttpContext!.Session;

    // Sends a request to the API, forwarding the saved session cookie.
    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);

        var cookie = Session.GetString(CookieKey);
        if (!string.IsNullOrEmpty(cookie))
            request.Headers.Add("Cookie", cookie);

        if (body != null)
            request.Content = JsonContent.Create(body);

        var response = await _http.SendAsync(request);

        // If the API sent a new session cookie (after login), remember it.
        if (response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            var first = values.FirstOrDefault();
            if (first != null)
                Session.SetString(CookieKey, first.Split(';')[0]);
        }

        return response;
    }

    public async Task<T?> GetAsync<T>(string url)
    {
        var response = await SendAsync(HttpMethod.Get, url);
        if (!response.IsSuccessStatusCode) return default;
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public void ForgetSession() => Session.Remove(CookieKey);
}