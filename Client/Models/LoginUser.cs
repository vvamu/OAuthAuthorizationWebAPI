namespace Client.Models;

public class LoginUser : BindableBase
{
    private string _Login;
    public string Login { get => _Login; set { SetProperty(ref _Login, value); } }

    private string _password;
    public string Password { get => _password; set { SetProperty(ref _password, value); } }

    public void CheckValid()
    {
        if (string.IsNullOrEmpty(_Login)) throw new Exception("Login is not valid");
        if (string.IsNullOrEmpty(_password)) throw new Exception("Password is not valid");
    }


}
