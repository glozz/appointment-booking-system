using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using AppointmentBooking.Web.ApiClients;
using AppointmentBooking.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services with global authorization policy
var policy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter(policy));
});

// HttpContextAccessor for accessing HttpContext in services
builder.Services.AddHttpContextAccessor();

// Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "AppointmentBooking.Auth";
        options.Cookie.HttpOnly = true;
        // Use SameAsRequest for development (allows HTTP), Always for production
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
            ? CookieSecurePolicy.SameAsRequest 
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

// Register AccessTokenHandler for forwarding auth tokens to API
builder.Services.AddTransient<AccessTokenHandler>();

// API Base URL configuration
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5000";

// Configure HttpClient for Auth API (login/register)
builder.Services.AddHttpClient("AuthApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

// Configure HttpClient for API with access token forwarding
builder.Services.AddHttpClient("BackendApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
})
.AddHttpMessageHandler<AccessTokenHandler>();

// Register API Client Services
builder.Services.AddScoped<IApiBranchService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<ApiBranchService>>();
    return new ApiBranchService(factory.CreateClient("BackendApi"), logger);
});

builder.Services.AddScoped<IApiServiceService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<ApiServiceService>>();
    return new ApiServiceService(factory.CreateClient("BackendApi"), logger);
});

builder.Services.AddScoped<IApiAvailabilityService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<ApiAvailabilityService>>();
    return new ApiAvailabilityService(factory.CreateClient("BackendApi"), logger);
});

builder.Services.AddScoped<IApiAppointmentService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<ApiAppointmentService>>();
    return new ApiAppointmentService(factory.CreateClient("BackendApi"), logger);
});

builder.Services.AddScoped<IApiConsultantService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<ApiConsultantService>>();
    return new ApiConsultantService(factory.CreateClient("BackendApi"), logger);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
