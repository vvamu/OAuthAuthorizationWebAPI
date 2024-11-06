using Microsoft.IdentityModel.Tokens;
namespace OAuthAuthorizationWebAPI.Helpers.Options;

public class JwtBearerOptions
{
    public string Authority { get; set; }
    public string ClaimsIssuer { get; set; }
    public string Audience { get; set; }
    public bool RequireHttpsMetadata { get; set; }
    public MyTokenValidationParameters TokenValidationParameters { get; set; }
}

public class MyTokenValidationParameters : TokenValidationParameters
{
    public string IssuerSigningKeyString { get; set; } // Новое свойство

}
