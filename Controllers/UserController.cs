using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OAuthAuthorization.Domain.Models;
using OAuthAuthorizationWebAPI.Persistence;
using OAuthAuthorizationWebAPI.Helpers.ViewModel;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using System.Security.Principal;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OAuthAuthorizationWebAPI.Controllers;

[Route("api")]
[ApiController]
[Authorize]
public class UserController : ControllerBase
{
    
    private readonly ApplicationDbContext _context;

    public UserController(ApplicationDbContext context)
    {
      
        _context = context;
    }

    [HttpGet]
    [Route("users/get")]
    public async Task<IActionResult> Get()
    {
        try
        {
            var users = await _context.Users.Select(x => new LoginViewModel() { Login = x.Login, Password = x.PasswordHash }).ToListAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
        }
    }
}

