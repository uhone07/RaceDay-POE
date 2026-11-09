using RaceDay.Web.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(30);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});
builder.Services.AddHttpClient<ApiClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!);
}).ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler { UseCookies = false };

    // Only used when running in Docker against the API's local development certificate.
    if (builder.Configuration.GetValue<bool>("ApiTrustAnyCertificate"))
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

    return handler;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();