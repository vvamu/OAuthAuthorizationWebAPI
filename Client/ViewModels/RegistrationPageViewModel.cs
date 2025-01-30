using Client.Helpers;
using Client.Models;
using System.Windows;

namespace Client.ViewModels;
public class RegistrationPageViewModel : BaseViewModel
{
    private LoginUser _LoginUser = new LoginUser();
    public LoginUser LoginUser
    {
        get => _LoginUser;
        set
        {
            SetProperty(ref _LoginUser, value);
        }
    }

    public DelegateCommand RegisterCommand { get; }

    public RegistrationPageViewModel(IRegionManager regionManager) : base(regionManager)
    {
        RegisterCommand = new DelegateCommand(async () =>
        {
            try
            {
                LoginUser.CheckValid();
                await ApiRepository.CreateUserAsync(LoginUser);
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