using Client.Helpers;

namespace Client.Models;

public class User
{
    public Guid Id { get; set; }
    public string Login { get; set; }
    public string PasswordHash { get; set; }
    public Token AccessToken { get; set; }

    public string RefreshToken { get; set; }


    public User(string identityToken, Token accessToken, string refreshToken)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;

        var identity = JwtTokenParser.ParseJwtToken(identityToken);
        Login = identity.GetValueOrDefault("Login");

    }

}



