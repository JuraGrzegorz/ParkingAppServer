using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SqlServer.Server;
using TheWebApiServer.Data;
using TheWebApiServer.Model;
using TheWebApiServer.Requests;

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
        private readonly DataContext _context;
        public IdentityApiController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IEmailSender<IdentityUser> emailSender,
            DataContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _context = context;
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

            //dodawnaie do roli
            await _userManager.AddToRoleAsync(user, "User");
            var curTreauser = new Treasure
            {
                User = user,
                Amount=0
            };
            await _context.treasure.AddAsync(curTreauser);
            await _context.SaveChangesAsync();

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
                await _signInManager.SignInAsync(user, isPersistent: true);


                return Ok(new {
                    accountType=await _userManager.GetRolesAsync(user),
                    userName=user.UserName
                });
            }
            else if (result.IsLockedOut)
            {
                return StatusCode(429, "Too many failed login attempts. Your account has been locked out. Please try again later.");
            }
            else if (result.RequiresTwoFactor)
            {
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


        [HttpPost("LogInByGoogle")]
        public async Task<IActionResult> LogInByGoogle([FromBody] string googleAuthToken)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(googleAuthToken);

                string userId = payload.Subject;
                string email = payload.Email;
                string name = payload.Name;
               

                // Check if the user already exists in the database by Google ID
                var user = await _userManager.FindByLoginAsync("Google", userId);

                if (user == null)
                {
                   
                    user = await _userManager.FindByEmailAsync(email);

                    if (user == null)
                    {
                        // Create a new user if not exists
                        user = new IdentityUser
                        {
                            UserName = email,
                            Email = email,
                        };

                        var result = await _userManager.CreateAsync(user);
                        if (!result.Succeeded)
                        {
                            return BadRequest(result.Errors);
                        }
                        await _userManager.AddToRoleAsync(user, "User");

                        var info = new UserLoginInfo("Google", userId, "Google");
                        await _userManager.AddLoginAsync(user, info);
                        await _userManager.AddToRoleAsync(user, "User");
                        var curTreauser = new Treasure
                        {
                            User = user,
                            Amount = 0
                        };
                        await _context.treasure.AddAsync(curTreauser);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        var info = new UserLoginInfo("Google", userId, "Google");
                        var result = await _userManager.AddLoginAsync(user, info);

                        if (!result.Succeeded)
                        {
                            return BadRequest(result.Errors);
                        }
                    }
                }
                else
                {
                    // Update existing user information
                   

                   /* var result = await _userManager.UpdateAsync(user);
                    if (!result.Succeeded)
                    {
                        return BadRequest(result.Errors);
                    }*/
                }

                await _signInManager.SignInAsync(user, true);


                return Ok(new
                {
                    accountType = await _userManager.GetRolesAsync(user),
                    userName = user.UserName
                });
            }
            catch (InvalidJwtException)
            {
                return Unauthorized(new { Message = "Invalid Google token." });
            }
        }
    }
}

