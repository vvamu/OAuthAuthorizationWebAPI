using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OAuthAuthorization.Domain.Models;
using OAuthAuthorizationWebAPI.Persistence;
using OAuthAuthorizationWebAPI.ViewModel;
using OpenIddict.Abstractions;

namespace OAuthAuthorizationWebAPI.Controllers;

[Route("api")]
[ApiController]
public class RegisterationController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _applicationDbContext;
    public RegisterationController(
          UserManager<ApplicationUser> userManager,
          ApplicationDbContext applicationDbContext)
    {
        _userManager = userManager;
        _applicationDbContext = applicationDbContext;
    }

    //
    // POST: /Account/Register
    [HttpPost]
    [Route("users/register")]
    public async Task<IActionResult> Register([FromBody] LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _applicationDbContext.Users.FirstOrDefaultAsync(x=>x.Login == model.Login.Trim());
            if (user != null)
            {
                return StatusCode(StatusCodes.Status409Conflict);
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