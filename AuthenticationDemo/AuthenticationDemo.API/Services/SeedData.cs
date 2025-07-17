using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using AuthenticationDemo.Data;

namespace AuthenticationDemo.Services;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();

        var manager = serviceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        // Register the API client
        if (await manager.FindByClientIdAsync("client_app") is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "client_app",
                ClientSecret = "client_app_secret",
                DisplayName = "API Client Application",
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "api"
                }
            });
        }

        // Register the Blazor client
        if (await manager.FindByClientIdAsync("blazor_client") is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "blazor_client",
                ClientSecret = "blazor_client_secret",
                DisplayName = "Blazor Web Application",
                RedirectUris = { new Uri("https://localhost:7215/callback") },
                PostLogoutRedirectUris = { new Uri("https://localhost:7215/") },
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Authorization,
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                    OpenIddictConstants.Permissions.GrantTypes.Password,
                    OpenIddictConstants.Permissions.ResponseTypes.Code,
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Scopes.Roles,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "api"
                }
            });
        }
    }
}