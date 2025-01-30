namespace Client.Helpers;

public class BaseViewModel : BindableBase, INavigationAware
{
    public IRegionManager _regionManager;
    protected AppSettings AppSettings { get; set; }
    public BaseViewModel(IRegionManager regionManager)
    {
        AppSettings = AppSettings.GetInstance;
        _regionManager = regionManager;
    }

    public virtual void OnNavigatedTo(NavigationContext navigationContext)
    {

    }

    public virtual bool IsNavigationTarget(NavigationContext navigationContext)
    {
        return true;
    }

    public virtual void OnNavigatedFrom(NavigationContext navigationContext)
    {

    }
}
