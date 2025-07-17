using AuthenticationDemo.Blazor;
using AuthenticationDemo.Blazor.Components;
using AuthenticationDemo.Blazor.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddSingleton<TokenHolder>();

// Add Authentication services
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        var config = builder.Configuration;
        options.LoginPath = config["Auth:LoginPath"] ?? "/login";
        options.AccessDeniedPath = config["Auth:AccessDeniedPath"] ?? "/accessdenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(int.TryParse(config["Auth:ExpireMinutes"], out var min) ? min : 30);
        options.SlidingExpiration = true;
    });

// Add Authorization services
builder.Services.AddAuthorization();

// Make AuthenticationState available to Blazor components
builder.Services.AddCascadingAuthenticationState();

// Optional: Add HttpContextAccessor if you need to access HttpContext directly in services
// builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<RegisterService>();
builder.Services.AddScoped<WeatherService>();
builder.Services.AddScoped<ApiService>();

// Configure HttpClient using BaseUrl from appsettings
var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7214";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
