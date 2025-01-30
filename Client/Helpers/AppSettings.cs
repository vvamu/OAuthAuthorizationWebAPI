using Client.Models;

namespace Client.Helpers;

public class AppSettings
{
    private static AppSettings instance;

    public event EventHandler CurrentUserChanged;
    private User _currentUser;
    public User CurrentUser
    {
        get { return _currentUser; }
        set
        {
            _currentUser = value;
            OnCurrentUserChanged();
        }
    }

    private AppSettings() { }
    public static AppSettings GetInstance { get { instance = instance ?? new AppSettings(); return instance; } }

    protected virtual void OnCurrentUserChanged()
    {
        CurrentUserChanged?.Invoke(this, EventArgs.Empty);
    }
}
