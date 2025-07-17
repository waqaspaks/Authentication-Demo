using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationDemo.Controllers;

/// <summary>
/// 
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    /// <summary>
    /// 
    /// </summary>
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{6,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number and one special character")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match")]
        public string ConfirmPassword { get; set; } = "";
    }
    /// <summary>
    /// 
    /// </summary>
    public class RegisterResponse
    {
        public bool Succeeded { get; set; }
        public string? Message { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    /// <summary>
    /// Registers a new user
    /// </summary>
    /// <param name="request">The registration request containing email and password</param>
    /// <returns>A response indicating success or failure with error messages</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        // Model validation is automatically handled by [ApiController]
        var response = new RegisterResponse();

        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            response.Succeeded = false;
            response.Message = "User with this email already exists";
            return BadRequest(response);
        }

        var user = new IdentityUser { UserName = request.Email, Email = request.Email };
        var result = await _userManager.CreateAsync(user, request.Password);

        if (result.Succeeded)
        {
            response.Succeeded = true;
            response.Message = "Registration successful";
            return Ok(response);
        }

        response.Succeeded = false;
        response.Message = "Registration failed";
        response.Errors = result.Errors.Select(e => e.Description).ToList();
        return BadRequest(response);
    }
    public class LoginRequest
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public class LoginResponse
    {
        public bool Succeeded { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return BadRequest(new LoginResponse { Succeeded = false, Message = "Invalid email or password" });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);
            return Ok(new LoginResponse { Succeeded = true, Message = "Login successful" });
        }

        return BadRequest(new LoginResponse { Succeeded = false, Message = "Invalid email or password" });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok();
    }
}