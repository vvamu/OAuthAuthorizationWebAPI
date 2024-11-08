using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OAuthAuthorization.Domain.Models;
using OAuthAuthorizationWebAPI.Helpers.ViewModel;
using OAuthAuthorizationWebAPI.Persistence;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
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
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly Helpers.Options.JwtBearerOptions _jwtBearerOptions;
    private readonly OpenIddictTokenManager<OpenIddictEntityFrameworkCoreToken> _tokenManager;
    private readonly IServiceProvider _serviceProvider;



    public AuthorizationController(SignInManager<ApplicationUser> signInManager, ApplicationDbContext context
        , IOptions<Helpers.Options.JwtBearerOptions> jwtBearerOptions, OpenIddictTokenManager<OpenIddictEntityFrameworkCoreToken> tokenManager, IServiceProvider serviceProvider)
    {
        _signInManager = signInManager;
        _context = context;
        _jwtBearerOptions = jwtBearerOptions.Value;
        _jwtBearerOptions.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtBearerOptions.TokenValidationParameters.IssuerSigningKeyString));
        _tokenManager = tokenManager;
        _serviceProvider = serviceProvider;
    }

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

        var jwtToken = await CreateJwtTokenAsync(user);
        var tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken validatedToken;
        var principal = tokenHandler.ValidateToken(jwtToken, _jwtBearerOptions.TokenValidationParameters, out validatedToken);

        //var ticket = new AuthenticationTicket(principal, new AuthenticationProperties(), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        //await HttpContext.SignInAsync(ticket.AuthenticationScheme,ticket.Principal, ticket.Properties); //RETURN FROM METHOD

        Request.Headers?.Add("Authorization", "Bearer " + jwtToken);

        var resultAuthentification = await HttpContext.AuthenticateAsync(OpenIddictConstants.Schemes.Bearer); // ?

        if (resultAuthentification?.Principal != null)
        {
            string accessToken = resultAuthentification.Properties.GetTokenValue("access_token");

            if (!string.IsNullOrEmpty(accessToken))
            {
                
                var tokenDescriptor = new OpenIddictTokenDescriptor
                {
                    Subject = user.Id.ToString()
                    ,Principal = principal
                    ,Type = OpenIddictConstants.TokenTypes.Bearer
                    ,ExpirationDate = DateTime.UtcNow + TimeSpan.FromMinutes(1)  
                    ,CreationDate = DateTime.UtcNow
                };
                var token = await _tokenManager.CreateAsync(tokenDescriptor);
                await _context.SaveChangesAsync();
                return Ok(accessToken);
            }
            else
            {
                return BadRequest(new { message = "Token is null or empty" });
            }
        }
        else
        {
            return BadRequest(new { message = "Token validation failed" });
        }

    }

    [HttpPost]
    [Route("users/refresh-token")]
    public async Task<IActionResult> RefreshToken(string token)
    {
        
        var tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken validatedToken;
        var principal = tokenHandler.ValidateToken(token, _jwtBearerOptions.TokenValidationParameters, out validatedToken);
        var login = principal.Identities.FirstOrDefault()?.Claims.FirstOrDefault()?.Value;
        //if (string.IsNullOrEmpty(login)) return BadRequest("Not valid token.");
        //var user = await _context.Users.FirstOrDefaultAsync(x => x.Login == login);
        //if (user == null) return BadRequest("User not found");

        var jwtToken = await CreateJwtTokenAsync(new ApplicationUser() { Login = login});
        tokenHandler.ValidateToken(jwtToken, _jwtBearerOptions.TokenValidationParameters, out validatedToken);

        Request.Headers?.Add("Authorization", "Bearer " + jwtToken);
        var resultAuthentification = await HttpContext.AuthenticateAsync(OpenIddictConstants.Schemes.Bearer);
        if(resultAuthentification.Succeeded)
        {
            return Ok(jwtToken);
        }
        else
        {
            return BadRequest("Token not set to user");
        }

        
    }

    private async Task<IActionResult> SignInByTokenAsync(ApplicationUser user)
    {
        try
        {
            var jwtToken = await CreateJwtTokenAsync(user);
            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken validatedToken;
            var principal = tokenHandler.ValidateToken(jwtToken, _jwtBearerOptions.TokenValidationParameters, out validatedToken);
            var identity = new ClaimsIdentity(principal.Claims, JwtBearerDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties();

            // ? 
            var resultSignIn = SignIn(principal, JwtBearerDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(JwtBearerDefaults.AuthenticationScheme, principal);
            await HttpContext.SignInAsync(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), authProperties);

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


        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, user.UserName));
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

