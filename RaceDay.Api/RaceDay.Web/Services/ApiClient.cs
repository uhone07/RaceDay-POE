using System.Net.Http.Headers;
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

    // Sends an image to one of the API's upload endpoints (the API stores it in Azure).
    // Returns the image URL, or an error message the page can show.
    public async Task<(string? Url, string? Error)> UploadImageAsync(string url, IFormFile file)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);

        var cookie = Session.GetString(CookieKey);
        if (!string.IsNullOrEmpty(cookie))
            request.Headers.Add("Cookie", cookie);

        var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        form.Add(fileContent, "File", file.FileName);
        request.Content = form;

        var response = await _http.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        if (response.IsSuccessStatusCode && result != null && result.TryGetValue("url", out var imageUrl))
            return (imageUrl, null);

        var message = result != null && result.TryGetValue("message", out var m)
            ? m
            : "The image could not be uploaded.";
        return (null, message);
    }

    public void ForgetSession() => Session.Remove(CookieKey);
}}