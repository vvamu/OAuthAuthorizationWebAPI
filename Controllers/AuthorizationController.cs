using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OAuthAuthorization.Domain.Models;
using OAuthAuthorizationWebAPI.Persistence;
using OAuthAuthorizationWebAPI.ViewModel;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using System.Security.Principal;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OAuthAuthorizationWebAPI.Controllers;

[Route("api")]
[ApiController]
public class AuthorizationController : ControllerBase
{
    private static ClaimsIdentity Identity = new ClaimsIdentity();
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public AuthorizationController(IOpenIddictApplicationManager applicationManager, IOpenIddictAuthorizationManager authorizationManager, IOpenIddictScopeManager scopeManager, 
        SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _scopeManager = scopeManager;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    #region Old
    /*

    [HttpPost("~/connect/token"), Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest();

        if (request.IsClientCredentialsGrantType())
        {

            var application = await _applicationManager.FindByClientIdAsync(request.ClientId) ??
                throw new InvalidOperationException("The application cannot be found.");

            // Create a new ClaimsIdentity containing the claims that
            // will be used to create an id_token, a token or a code.
            var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);

            identity.SetClaim(Claims.Subject, await _applicationManager.GetClientIdAsync(application));
            identity.SetClaim(Claims.Name, await _applicationManager.GetDisplayNameAsync(application));

            identity.SetDestinations(static claim => claim.Type switch
            {
                // Allow the "name" claim to be stored in both the access and identity tokens
                // when the "profile" scope was granted (by calling principal.SetScopes(...)).
                Claims.Name when claim.Subject.HasScope(Scopes.Profile)
                    => [Destinations.AccessToken, Destinations.IdentityToken],

                // Otherwise, only store the claim in the access tokens.
                _ => [Destinations.AccessToken]
            });

            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new NotImplementedException("The specified grant is not implemented.");
    }

    [HttpPost]
    [Route("authenticate")]
    public IActionResult AuthenticateUser(LoginViewModel login)
    {
        // Implementation for user authentication
        return Ok();
    }

    [HttpPost]
    [Route("refresh-token")]
    public IActionResult RefreshToken()
    {
        // Implementation for refreshing token
        return Ok();
    }
    */
    #endregion

    [HttpPost]
    [Route("users/token")]
    public async Task<IActionResult> ConnectToken()
    {
        try
        {
            var openIdConnectRequest = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            Identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, Claims.Name, Claims.Role);
            ApplicationUser? user = null;
            AuthenticationProperties properties = new();

            if (openIdConnectRequest.IsClientCredentialsGrantType())
            {
                throw new NotImplementedException();
            }
            else if (openIdConnectRequest.IsPasswordGrantType())
            {
                user = await _context.Users.FirstOrDefaultAsync(x=>x.Login == openIdConnectRequest.Username);

                if (user == null)
                {
                    return BadRequest(new OpenIddictResponse
                    {
                        Error = Errors.InvalidGrant,
                        ErrorDescription = "User does not exist"
                    });
                }

                var result = await _signInManager.PasswordSignInAsync(user.UserName, openIdConnectRequest.Password, false, lockoutOnFailure: false);
                if (!result.Succeeded)
                {
                    return BadRequest(new OpenIddictResponse
                    {
                        Error = Errors.InvalidGrant,
                        ErrorDescription = "Username or password is incorrect"
                    });
                    
                }

                //// Getting scopes from user parameters (TokenViewModel) and adding in Identity 
                Identity.SetScopes(openIdConnectRequest.GetScopes());

                // Getting scopes from user parameters (TokenViewModel)
                // Checking in OpenIddictScopes tables for matching resources
                // Adding in Identity
                //var listResources = await _scopeManager.ListResourcesAsync(Identity.GetScopes()).;
                //Identity.SetResources(.GetAsyncEnumerator().Current.ToList());


                // Add Custom claims
                // sub claims is mendatory
                //Identity.AddClaim(new Claim(Claims.Subject, user.Id));
                Identity.AddClaim(new Claim(Claims.Audience, "Resourse"));

                Identity.SetDestinations(GetDestinations);
            }
            else if (openIdConnectRequest.IsRefreshTokenGrantType())
            {
                throw new NotImplementedException();
            }
            else
            {
                return BadRequest(new
                {
                    error = Errors.UnsupportedGrantType,
                    error_description = "The specified grant type is not supported."
                });
            }

            var signInResult = SignIn(new ClaimsPrincipal(Identity), properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            return signInResult;
        }
        catch (Exception ex)
        {
            return BadRequest(new OpenIddictResponse()
            {
                Error = Errors.ServerError,
                ErrorDescription = "Invalid login attempt"
            });
        }
    }

    #region Private Methods

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        // Note: by default, claims are NOT automatically included in the access and identity tokens.
        // To allow OpenIddict to serialize them, you must attach them a destination, that specifies
        // whether they should be included in access tokens, in identity tokens or in both.

        return claim.Type switch
        {
            Claims.Name or
            Claims.Subject
               => new[] { Destinations.AccessToken, Destinations.IdentityToken },

            _ => new[] { Destinations.AccessToken },
        };
    }

    #endregion
}

