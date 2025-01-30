using Microsoft.Extensions.DependencyInjection;
using OAuthAuthorization.Testing.ViewModels;
using OpenIddict.Client;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OAuthAuthorization.Testing.Configurations;

public static class ApiRepository
{
    public static IServiceProvider Provider;
    public static async Task CreateConnectClientToServer(Uri issuer, string clientId, string clientSecret)
    {
        using var client = Provider.GetRequiredService<HttpClient>();

        var clientJson = new
        {
            Issuer = issuer,
            ClientId = clientId,
            ClientSecret = clientSecret
        };
        var stringJson = JsonSerializer.Serialize(clientJson);
        var url = "https://localhost:7292/api/client/register";
        var content = new StringContent(stringJson, Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = content;

        using var response = await client.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            string responseContent = await response.Content.ReadAsStringAsync();
            throw new Exception(responseContent);

        }
    }

    //ConnectToServerAndGetTokenAsync
    public static async Task<(string, string, string, DateTimeOffset?)> AuthenticateClientAndUser(LoginViewModel loginUser)
    {
        var service = Provider.GetRequiredService<OpenIddictClientService>();

        var request = new OpenIddictClientModels.PasswordAuthenticationRequest
        {
            Username = loginUser.Login,
            Password = loginUser.Password,
            Issuer = new Uri("https://localhost:7292"),
            Scopes = new List<string>() { Scopes.OpenId, Scopes.OfflineAccess }
        };

        var result = await service.AuthenticateWithPasswordAsync(request);

        var accessToken = result.AccessToken;
        var accessTokenExpireDate = result.AccessTokenExpirationDate;
        var identityToken = result.IdentityToken;
        var refreshToken = result.RefreshToken;

        return (identityToken, refreshToken, accessToken, accessTokenExpireDate);
    }

    public static async Task<List<LoginViewModel>> GetUsersAsync(string token)
    {

        using var client = Provider.GetRequiredService<HttpClient>();
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:7292/api/users/get");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            string responseContent = await response.Content.ReadAsStringAsync();
            throw new Exception(responseContent);
        }
        var resultJsonString = await response.Content.ReadAsStringAsync();
        var loginUsers = JsonSerializer.Deserialize<List<LoginViewModel>>(resultJsonString.Replace("login", "Login").Replace("password", "Password"));

        return loginUsers ?? new List<LoginViewModel>();
    }

    public static async Task<LoginViewModel> CreateUserAsync(LoginViewModel loginUser)
    {
        using var client = Provider.GetRequiredService<HttpClient>();
        var json = JsonSerializer.Serialize(loginUser);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("https://localhost:7292/api/users/register", content);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            string responseContent = await response.Content.ReadAsStringAsync();
            throw new Exception(responseContent);
        }

        return loginUser;
    }

    public static async Task<(string, string, string, DateTimeOffset?)> RefreshTokenAsync(string refreshToken)
    {
        var service = Provider.GetRequiredService<OpenIddictClientService>();
        var result = await service.AuthenticateWithRefreshTokenAsync(new()
        {
            RefreshToken = refreshToken,
            Issuer = new Uri("https://localhost:7292"),
            Scopes = new List<string>() { Scopes.OpenId, Scopes.OfflineAccess }
        });

        var accessToken = result.AccessToken;
        var accessTokenExpireDate = result.AccessTokenExpirationDate;
        var identityToken = result.IdentityToken;
        var refreshTokenResult = result.RefreshToken;
        return (identityToken, accessToken, refreshTokenResult, accessTokenExpireDate); ;

    }

}
