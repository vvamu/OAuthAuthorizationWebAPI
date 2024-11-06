using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OAuthAuthorization.Domain.Models;
using OAuthAuthorizationWebAPI.Helpers.ViewModel;
using OAuthAuthorizationWebAPI.Persistence;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OAuthAuthorizationWebAPI.Controllers;

[Route("api")]
[ApiController]
public class RegisterationController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    public RegisterationController(
          UserManager<ApplicationUser> userManager,
          ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [HttpPost]
    [Route("users/register")]
    public async Task<IActionResult> Register([FromBody] LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x=>x.Login == model.Login.Trim());
            if (user != null)
            {
                return StatusCode(StatusCodes.Status409Conflict, new { message = "User with this login already exists." });
            }

            user = new ApplicationUser { Login = model.Login.Trim(), UserName = model.Login.Trim() };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                return Ok();
            }
            AddErrors(result);

        }

        return BadRequest(ModelState);
    }

    #region Helpers

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    #endregion
}