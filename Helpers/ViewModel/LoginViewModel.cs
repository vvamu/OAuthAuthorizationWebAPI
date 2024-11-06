using System.ComponentModel.DataAnnotations;

namespace OAuthAuthorizationWebAPI.Helpers.ViewModel;

public class LoginViewModel
{
    [Required]
    public string Login { get; set; }
    [Required]
    public string Password { get; set; }

}
