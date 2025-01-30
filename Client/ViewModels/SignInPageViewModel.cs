using Client.Helpers;
using Client.Models;
using System.Windows;

namespace Client.ViewModels;
public class SignInPageViewModel : BaseViewModel
{
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
    public DelegateCommand SignInCommand { get; }
    public SignInPageViewModel(IRegionManager regionManager) : base(regionManager)
    {
        SignInCommand = new DelegateCommand(async () =>
        {
            try
            {
                LoginUser.CheckValid();
                var (identityToken, refreshToken, accessToken) = await ApiRepository.AuthenticateClientAndUser(LoginUser);
                AppSettings.CurrentUser = new User(identityToken, accessToken, refreshToken);

                _regionManager.RequestNavigate("ContentRegionMainWindow", "MainPage");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }, () => true
        );
    }



}