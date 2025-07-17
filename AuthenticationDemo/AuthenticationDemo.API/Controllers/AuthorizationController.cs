using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthenticationDemo.Controllers;
/// <summary>
/// 
/// </summary>
public class AuthorizationController : Controller
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;

    public AuthorizationController(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    [HttpPost("~/connect/token")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request == null)
        {
            return BadRequest();
        }

        // Handle password grant type
        if (request.IsPasswordGrantType())
        {
            var user = await _userManager.FindByNameAsync(request.Username);
            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                return Forbid();
            }

            var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                Claims.Name, Claims.Role);

            var userId = await _userManager.GetUserIdAsync(user);
            var email = await _userManager.GetEmailAsync(user);
            var userName = await _userManager.GetUserNameAsync(user);

            var subjectClaim = new Claim(Claims.Subject, userId);
            subjectClaim.SetDestinations(Destinations.AccessToken, Destinations.IdentityToken);
            identity.AddClaim(subjectClaim);

            if (email != null)
            {
                var emailClaim = new Claim(Claims.Email, email);
                emailClaim.SetDestinations(Destinations.AccessToken, Destinations.IdentityToken);
                identity.AddClaim(emailClaim);
            }
            if (userName != null)
            {
                var nameClaim = new Claim(Claims.Name, userName);
                nameClaim.SetDestinations(Destinations.AccessToken, Destinations.IdentityToken);
                identity.AddClaim(nameClaim);
            }

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                var roleClaim = new Claim(Claims.Role, role);
                roleClaim.SetDestinations(Destinations.AccessToken, Destinations.IdentityToken);
                identity.AddClaim(roleClaim);
            }

            // Only add requested scopes
            var scopes = request.GetScopes().ToList();
            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes(scopes);
            foreach (var scope in scopes)
            {
                var scopeClaim = new Claim("scope", scope);
                scopeClaim.SetDestinations(Destinations.AccessToken);
                identity.AddClaim(scopeClaim);
            }

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsClientCredentialsGrantType())
        {
            var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            identity.AddClaim(Claims.Subject, request.ClientId ?? string.Empty);
            identity.AddClaim(Claims.Name, request.ClientId ?? string.Empty);
            identity.AddClaim(Claims.Role, "api_access");

            var claimsPrincipal = new ClaimsPrincipal(identity);
            claimsPrincipal.SetScopes(request.GetScopes());

            return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsAuthorizationCodeGrantType())
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (result?.Principal == null)
            {
                return Forbid();
            }
            return SignIn(result.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsRefreshTokenGrantType())
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (result?.Principal == null)
            {
                return Forbid();
            }
            return SignIn(result.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new NotImplementedException("The specified grant type is not implemented.");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request == null)
        {
            return BadRequest();
        }

        var result = await HttpContext.AuthenticateAsync();
        if (result == null || !result.Succeeded || result.Principal == null)
        {
            return Challenge(properties: new AuthenticationProperties
            {
                RedirectUri = Request.PathBase + Request.Path + QueryString.Create(
                    Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList())
            });
        }

        var user = await _userManager.GetUserAsync(result.Principal);
        if (user == null)
        {
            return Forbid();
        }

        var identity = new ClaimsIdentity(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            Claims.Name,
            Claims.Role);

        var userId = await _userManager.GetUserIdAsync(user);
        var email = await _userManager.GetEmailAsync(user);
        var userName = await _userManager.GetUserNameAsync(user);

        identity.AddClaim(Claims.Subject, userId);
        if (email != null)
        {
            identity.AddClaim(Claims.Email, email);
        }
        if (userName != null)
        {
            identity.AddClaim(Claims.Name, userName);
        }

        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            identity.AddClaim(Claims.Role, role);
        }

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [HttpGet("~/connect/userinfo")]
    public async Task<IActionResult> Userinfo()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var userId = await _userManager.GetUserIdAsync(user);
        var email = await _userManager.GetEmailAsync(user);
        var userName = await _userManager.GetUserNameAsync(user);

        var claims = new Dictionary<string, object>
        {
            [Claims.Subject] = userId
        };

        if (email != null)
        {
            claims[Claims.Email] = email;
        }
        if (userName != null)
        {
            claims[Claims.Name] = userName;
        }

        return Ok(claims);
    }
}