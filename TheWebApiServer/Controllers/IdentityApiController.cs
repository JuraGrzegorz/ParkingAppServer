using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace TheWebApiServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class IdentityApiController : ControllerBase
    {
        private static readonly EmailAddressAttribute _emailAddressAttribute = new();

        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IEmailSender<IdentityUser> _emailSender;

        public IdentityApiController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IEmailSender<IdentityUser> emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest registration)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var email = registration.Email;

            if (string.IsNullOrEmpty(email) || !_emailAddressAttribute.IsValid(email))
            {
                ModelState.AddModelError(nameof(registration.Email), "Invalid email format.");
                return BadRequest(ModelState);
            }

            var user = new IdentityUser { UserName = email, Email = email };
            var result = await _userManager.CreateAsync(user, registration.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return BadRequest(ModelState);
            }

            return Ok("User registered successfully.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest login)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _signInManager.PasswordSignInAsync(login.Email, login.Password, true, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                
                var user = await _userManager.FindByEmailAsync(login.Email);
                await _signInManager.SignInAsync(user, isPersistent: true); // user to zmienna reprezentująca zalogowanego użytkownika

                // Pobierz ciasteczko sesji
                var sessionCookie = HttpContext.Response.Headers["Set-Cookie"];

                return Ok(new { message = "Login successful", sessionCookie });
            }
            else if (result.IsLockedOut)
            {
                return StatusCode(429, "Too many failed login attempts. Your account has been locked out. Please try again later.");
            }
            else if (result.RequiresTwoFactor)
            {
                // Redirect to two-factor authentication page or return custom response
                return StatusCode(403, "Two-factor authentication required.");
            }
            else
            {
                return Unauthorized("Invalid email or password.");
            }
        }


        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync().ConfigureAwait(false);
            return Ok();
        }
    }
}

