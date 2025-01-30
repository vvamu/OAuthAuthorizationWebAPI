using Client.Helpers;
using Client.Models;

namespace Client.ViewModels;
public class MainWindowViewModel : BaseViewModel
{
    private string _windowTitle = "";
    public string WindowTitle
    {
        get { return _windowTitle; }
        set
        {
            _windowTitle = value;
            SetProperty(ref _windowTitle, value);
        }
    }

    private LoginUser _LoginUser = new LoginUser();
    public LoginUser LoginUser
    {
        get => _LoginUser;
        set
        {
            LoginUser.CheckValid();
            SetProperty(ref _LoginUser, value);
        }
    }

    public MainWindowViewModel(IRegionManager regionManager) : base(regionManager)
    {
        //var username = AppSettings.CurrentUser?.Login ?? "No user was login";
        WindowTitle = "OAuth Client "; //+ username;
        AppSettings.CurrentUserChanged += AppSettings_CurrentUserChanged;

        UpdateWindowTitle();
    }
    private void AppSettings_CurrentUserChanged(object sender, EventArgs e)
    {
        UpdateWindowTitle();
    }

    private void UpdateWindowTitle()
    {
        var username = AppSettings.CurrentUser == null ? "" : "" + AppSettings.CurrentUser?.Login;
        WindowTitle = "OAuth Client" + username;
        LoginUser.Login = username;
    }

}


