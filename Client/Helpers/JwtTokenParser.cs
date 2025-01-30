using System.IdentityModel.Tokens.Jwt;


namespace Client.Helpers;

public static class JwtTokenParser
{
    public static Dictionary<string, string> ParseJwtToken(string jwtToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var resultDictionary = new Dictionary<string, string>();
        var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

        if (jsonToken != null)
        {
            foreach (System.Security.Claims.Claim claim in jsonToken.Claims)
            {
                resultDictionary.Add(claim.Type, claim.Value);
            }
        }
        return resultDictionary;
    }
}
