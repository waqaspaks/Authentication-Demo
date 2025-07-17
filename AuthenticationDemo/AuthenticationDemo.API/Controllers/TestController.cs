using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;

namespace AuthenticationDemo.Controllers;

/// <summary>
/// 
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class TestController : ControllerBase
{
    [HttpGet]
    //[Authorize(Policy = "ApiScope")]
    public IActionResult Get()
    {
        return Ok(new { message = "This is a protected API endpoint" });
    }
}