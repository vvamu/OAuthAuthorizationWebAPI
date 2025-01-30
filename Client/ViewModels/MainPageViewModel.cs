using Client.Helpers;
using System.Windows;

namespace Client.ViewModels;
public class MainPageViewModel : BaseViewModel
{
    public DelegateCommand NavigateToMainPageCommand { get; }
    public DelegateCommand NavigateToUsersPageCommand { get; }
    public DelegateCommand NavigateToSignPageCommand { get; }
    public DelegateCommand NavigateToRegistrationPageCommand { get; }

    public MainPageViewModel(IRegionManager regionManager) : base(regionManager)
    {
        NavigateToMainPageCommand = new DelegateCommand(async () =>
            {
                try
                {
                    _regionManager.RequestNavigate("ContentRegionMainWindow", "MainPage");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }, () => true
        );
        NavigateToUsersPageCommand = new DelegateCommand(async () =>
        {
            try
            {
                _regionManager.RequestNavigate("ContentRegionMainWindow", "UsersPage");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }, () => true
        );

        NavigateToSignPageCommand = new DelegateCommand(async () =>
        {
            try
            {
                _regionManager.RequestNavigate("ContentRegionMainWindow", "SignInPage");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }, () => true
        );

        NavigateToRegistrationPageCommand = new DelegateCommand(async () =>
        {
            try
            {
                _regionManager.RequestNavigate("ContentRegionMainWindow", "RegistrationPage");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }, () => true
        );

    }
}