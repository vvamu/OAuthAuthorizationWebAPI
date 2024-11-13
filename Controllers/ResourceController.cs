using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OAuthAuthorization.Domain.Models;
using OAuthAuthorizationWebAPI.Helpers;
using OAuthAuthorizationWebAPI.Helpers.ViewModel;
using OAuthAuthorizationWebAPI.Persistence;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OAuthAuthorizationWebAPI.Controllers;

[Route("api")]
[ApiController]

public class ResourceController : ControllerBase
{

    private readonly IApplicationUserService _userService;
    private readonly IOpenIddictApplicationManager _openIddictApplicationManager;
    private readonly IOpenIddictTokenManager _openIddictTokenManager;

    public ResourceController(IApplicationUserService userService , IOpenIddictApplicationManager openIddictApplicationManager, IOpenIddictTokenManager openIddictTokenManager)
    {
        _userService = userService;
        _openIddictApplicationManager = openIddictApplicationManager;
        _openIddictTokenManager = openIddictTokenManager;
    }

    [HttpPost]
    [Route("users/register")]
    public async Task<IActionResult> Register([FromBody] LoginViewModel model)
    {
        LoginViewModel result;
        try
        {
            result = await _userService.CreateAsync(model);
        }
        catch (Exception ex) 
        {

            return BadRequest(ex.Message);

        }

        return Ok(result);
    }

    [HttpGet]
    [Route("users/get")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]

    public async Task<IActionResult> Get()
    {
        var userAccessToken = HttpContext.Request.Headers.Authorization;
        var accessToken = userAccessToken.ToString().Replace("Bearer ","");

        IEnumerable<LoginViewModel> users = await _userService.GetAllAsync();
        return Ok(users);

    }
}

