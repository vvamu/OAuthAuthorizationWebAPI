namespace OAuthAuthorizationWebAPI.Helpers.ViewModel;

public class LoginViewModel
{
    public string Login { get; set; }

    public string Password { get; set; }

    public void CheckValid()
    {
        if (string.IsNullOrEmpty(Login)) throw new Exception("Login is not valid");
        if (string.IsNullOrEmpty(Password)) throw new Exception("Password is not valid");
    }
}
