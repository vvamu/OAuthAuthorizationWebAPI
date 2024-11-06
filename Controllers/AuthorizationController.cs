using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OAuthAuthorization.Domain.Models;
using OAuthAuthorizationWebAPI.Helpers.ViewModel;
using OAuthAuthorizationWebAPI.Persistence;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
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
    private readonly Helpers.Options.JwtBearerOptions _jwtBearerOptions;



    public AuthorizationController(IOpenIddictApplicationManager applicationManager, IOpenIddictAuthorizationManager authorizationManager, IOpenIddictScopeManager scopeManager,
        SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ApplicationDbContext context, IOptions<Helpers.Options.JwtBearerOptions> jwtBearerOptions)
    {
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _scopeManager = scopeManager;
        _signInManager = signInManager;
        _userManager = userManager;
        _context = context;
        _jwtBearerOptions = jwtBearerOptions.Value;
        _jwtBearerOptions.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtBearerOptions.TokenValidationParameters.IssuerSigningKeyString));
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
    public async Task<IActionResult> Authenticate([FromBody] LoginViewModel model)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Login == model.Login.Trim());
        if (user == null)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidGrant,
                ErrorDescription = "User does not exist with this login"
            });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidGrant,
                ErrorDescription = "Password is incorrect"
            });

        }

        try
        {
            var jwtToken = await CreateJwtTokenAsync(user);
            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken validatedToken;
            var principal = tokenHandler.ValidateToken(jwtToken, _jwtBearerOptions.TokenValidationParameters, out validatedToken);

            var resultSignIn = SignIn(principal, JwtBearerDefaults.AuthenticationScheme);

            //await HttpContext.SignInAsync(JwtBearerDefaults.AuthenticationScheme, principal);

            return Ok(jwtToken);
        }
        catch (SecurityTokenValidationException ex)
        {
            return BadRequest(new { message = "Token validation failed", error = ex.Message });
        }
    }
    private async Task<string> CreateJwtTokenAsync(ApplicationUser user)
    {
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);


        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, user.Login));
        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtBearerOptions.TokenValidationParameters.IssuerSigningKeyString);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(identity.Claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Audience = _jwtBearerOptions?.Audience,
            Issuer = _jwtBearerOptions?.ClaimsIssuer,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(token);
        return jwtToken;
    }


}

