using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuthAuthorizationWebAPI.Helpers;
using OAuthAuthorizationWebAPI.Helpers.ViewModel;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;

namespace OAuthAuthorizationWebAPI.Controllers;

[Route("api")]
[ApiController]

public class ResourceController : ControllerBase
{

    private readonly IApplicationUserService _userService;
    private readonly IOpenIddictApplicationManager _openIddictApplicationManager;
    private readonly IOpenIddictTokenManager _openIddictTokenManager;

    public ResourceController(IApplicationUserService userService, IOpenIddictApplicationManager openIddictApplicationManager, IOpenIddictTokenManager openIddictTokenManager)
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
        var accessToken = userAccessToken.ToString().Replace("Bearer ", "");

        IEnumerable<LoginViewModel> users = await _userService.GetAllAsync();
        return Ok(users);

    }
}

