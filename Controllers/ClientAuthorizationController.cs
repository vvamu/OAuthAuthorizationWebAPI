using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OAuthAuthorizationWebAPI.Helpers;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Json;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OAuthAuthorizationWebAPI.Controllers;

[Route("api")]
[ApiController]
public class ClientAuthorizationController : ControllerBase
{
    private readonly IApplicationUserService _userService;
    private readonly IOpenIddictApplicationManager _openIddictApplicationManager;
    private readonly IOpenIddictTokenManager _openIddictTokenManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    public ClientAuthorizationController(IApplicationUserService userService, IOpenIddictApplicationManager openIddictApplicationManager, 
        IOpenIddictTokenManager openIddictTokenManager, IOpenIddictScopeManager scopeManager)
    {
        _userService = userService;
        _openIddictApplicationManager = openIddictApplicationManager;
        _openIddictTokenManager = openIddictTokenManager;
        _scopeManager = scopeManager;
    }

    [HttpPost]
    [Route("client/register")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterNewClient()
    {
        try
        {
            using var reader = new StreamReader(HttpContext.Request.Body);
            var body = await reader.ReadToEndAsync();

            using JsonDocument doc = JsonDocument.Parse(body);
            JsonElement root = doc.RootElement;

            if (root.TryGetProperty("issuer", out JsonElement issuerElement) &&
                root.TryGetProperty("clientId", out JsonElement clientIdElement) &&
                root.TryGetProperty("clientSecret", out JsonElement clientSecretElement))
            {
                string issuer = issuerElement.GetString();
                string clientId = clientIdElement.GetString();
                string clientSecret = clientSecretElement.GetString();

                var client = await _openIddictApplicationManager.FindByClientIdAsync(clientId);
                if (client != null) return StatusCode(409, "Client already connected");

                var result = await _openIddictApplicationManager.CreateAsync(new OpenIddictApplicationDescriptor
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                    DisplayName = clientId,

                    Permissions =
                            {
                            Permissions.Endpoints.Token,
                            Permissions.Endpoints.Authorization,

                            Permissions.GrantTypes.Password,
                            Permissions.GrantTypes.RefreshToken,

                            }
                });
                
            }
        }
        catch (Exception ex) 
        {
            return BadRequest("Client app wasn't register. Exception: " + ex.Message.ToString());
        }

        return Ok();
    }

    [HttpPost]
    [Route("client/token")]
    [AllowAnonymous]
    public async Task<IActionResult> AuthenticateClientPasswordFlow()
    {
        try
        {
            var openIdConnectRequest = HttpContext.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");
            
            if (!openIdConnectRequest.IsPasswordGrantType() && !openIdConnectRequest.IsRefreshTokenGrantType()) 
                throw new NotImplementedException("The specified grant type is not implemented.");
            if(openIdConnectRequest.IsRefreshTokenGrantType())
            {
                var identityRefresh = await RefreshTokenAsync(); 
                return SignIn(new ClaimsPrincipal(identityRefresh), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            }
            var client = await _openIddictApplicationManager.FindByClientIdAsync(openIdConnectRequest.ClientId);
            if (client == null) throw new Exception("ClientId or Password is not valid");


            var identity = new ClaimsIdentity(
                    authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                    nameType: Claims.Name,
                    roleType: Claims.Role);

            identity.SetClaim(Claims.Subject, await _openIddictApplicationManager.GetClientIdAsync(client));
            identity.SetClaim(Claims.Name, await _openIddictApplicationManager.GetDisplayNameAsync(client));

            var userDb = await _userService.AuthenticateAsync(new Helpers.ViewModel.LoginViewModel() {  Login =openIdConnectRequest.Username , Password = openIdConnectRequest.Password});
            identity.SetClaim(Claims.Audience, userDb.Id.ToString());
            identity.SetClaim("Login", userDb.Login); 
            identity.SetClaim("PasswordHash", userDb.PasswordHash);
            identity.SetDestinations((claim) => [Destinations.AccessToken,Destinations.IdentityToken]);
            identity.SetScopes(openIdConnectRequest.GetScopes());

            var principal = new ClaimsPrincipal(identity);
            
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            

        }
        catch (Exception ex)
        {
            return BadRequest(ex.ToString());
        }
    }

    

    
    [HttpPost]
    [Route("client/refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken(string token)
    {
        try
        {
            var identity = await RefreshTokenAsync();
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await httpClient.GetAsync("https://localhost:7292/api/users/get");
            if (!response.IsSuccessStatusCode)
                throw new Exception("Failed to access the protected endpoint");
            var refreshedToken = CreateRefreshTokenInDatabase(identity);
            return Ok(refreshedToken);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.ToString());
        }
    }


    private async Task<ClaimsIdentity> RefreshTokenAsync()
    {

        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        if (!result.Succeeded)
            throw new Exception("Failed authenticate");

        var id = Guid.Parse(result.Principal.GetClaim(Claims.Audience));
        var user = await _userService.GetAsync(id);


        var identity = new ClaimsIdentity(result.Principal.Claims,
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.SetClaim(Claims.Subject, result.Principal.GetClaim(Claims.Subject));
        identity.SetClaim(Claims.Name, result.Principal.GetClaim(Claims.Name));

        identity.SetClaim(Claims.Audience, user.Id.ToString());
        identity.SetClaim("Login", user.Login);
        identity.SetClaim("PasswordHash", user.PasswordHash);
        identity.SetDestinations((claim) => [Destinations.AccessToken, Destinations.IdentityToken]);
        return identity;
    }

    private async Task<object> CreateRefreshTokenInDatabase(ClaimsIdentity identity)
    {

        var rT = _openIddictTokenManager.FindByApplicationIdAsync(identity.GetClaim(Claims.Subject));
        object result;
        //var lastRefreshToken 
        var token = new OpenIddictTokenDescriptor
        {
            ApplicationId = identity.GetClaim(Claims.Subject),
            AuthorizationId = "",
            Subject = identity.GetClaim(Claims.Subject),
            Type = "refresh-token",
            CreationDate = DateTime.UtcNow,
            ExpirationDate = DateTime.UtcNow.AddDays(1),
        };
        if (rT != null)
        {

            result = _openIddictTokenManager.UpdateAsync(identity);
        }
        else
        {
            result = await _openIddictTokenManager.CreateAsync(token);
        }
        return result;



    }

}
