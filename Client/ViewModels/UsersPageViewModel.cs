using Client.Helpers;
using Client.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace Client.ViewModels;
public class UsersPageViewModel : BaseViewModel
{
    private ObservableCollection<LoginUser> _users;

    public ObservableCollection<LoginUser> Users { get => _users; set { SetProperty(ref _users, value); } }

    public bool IsCanNavigate = true;
    public UsersPageViewModel(IRegionManager regionManager) : base(regionManager)
    {

        LoadUsersAsync();

    }

    private async void LoadUsersAsync()
    {
        try
        {
            var usersWithFirstToken = await ApiRepository.GetUsersAsync(AppSettings.CurrentUser?.AccessToken.Value);
            Users = new ObservableCollection<LoginUser>(usersWithFirstToken);
            IsCanNavigate = true;

        }
        catch (Exception ex) { MessageBox.Show(ex.Message); _regionManager.RequestNavigate("ContentRegionMainWindow", "MainPage"); IsCanNavigate = false; }

    }

    public override bool IsNavigationTarget(NavigationContext navigationContext)
    {
        LoadUsersAsync();
        return IsCanNavigate;
    }
}