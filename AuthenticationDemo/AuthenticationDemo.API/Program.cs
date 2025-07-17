using AuthenticationDemo.Data;
using AuthenticationDemo.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v1",
        Title = "AuthenticationDemo API",
        Description = "API for AuthenticationDemo with OpenIddict and JWT support"
    });

    // Enable XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.UseOpenIddict();
});

// Add Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure OpenIddict
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiScope", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "scope" &&
                (c.Value == "api" || c.Value.Split(' ').Contains("api"))));
    });
    options.AddPolicy("EmailScope", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", Scopes.Email);
    });
});
builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("/connect/token")
               .SetAuthorizationEndpointUris("/connect/authorize");

        // Userinfo endpoint is not available in v7 by default. Remove or implement manually if needed.

        options.AllowPasswordFlow()
               .AllowClientCredentialsFlow()
               .AllowAuthorizationCodeFlow()
               .AllowRefreshTokenFlow();

        options.RegisterScopes(
            Scopes.OpenId,
            Scopes.Email,
            Scopes.Profile,
            Scopes.Roles,
            "api"
        );

        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        // JWTs are now default in v7, no need for UseJsonWebTokens()

        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough()
               .EnableAuthorizationEndpointPassthrough();
        // .EnableUserinfoEndpointPassthrough() removed in v7
    })
    .AddValidation(options =>
    {
        var issuer = builder.Configuration["OpenIddict:Issuer"] ?? "https://localhost:7071/";
        options.SetIssuer(issuer); // Must match token issuer
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "https://localhost:7215" };
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();
// Debug logging middleware for incoming JWTs
app.Use(async (context, next) =>
{
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
    if (!string.IsNullOrEmpty(authHeader))
    {
        Console.WriteLine($"Authorization header: {authHeader}");
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            Console.WriteLine($"User authenticated: {context.User.Identity.Name}");
            foreach (var claim in context.User.Claims)
            {
                Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
            }
        }
        else
        {
            Console.WriteLine("User not authenticated");
        }
    }
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowBlazorClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Initialize the database with seed data
using (var scope = app.Services.CreateScope())
{
    await AuthenticationDemo.Services.SeedData.InitializeAsync(scope.ServiceProvider);
}

app.Run();
