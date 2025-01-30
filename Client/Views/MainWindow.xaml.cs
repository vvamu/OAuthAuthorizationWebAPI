using Client.Helpers;
using System.Windows;
using System.Windows.Threading;

namespace Client.Views;
public partial class MainWindow : Window
{
    private DispatcherTimer timer;

    public MainWindow(IRegionManager regionManager)
    {
        regionManager.RegisterViewWithRegion("ContentRegionMainWindow", typeof(MainPage));
        InitializeComponent();

    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        timer = new DispatcherTimer();
        timer.Interval = TimeSpan.FromSeconds(20);
        timer.Tick += Timer_Tick;
        timer.Start();
    }

    private async void Timer_Tick(object sender, EventArgs e)
    {
        if (AppSettings.GetInstance.CurrentUser == null) { return; }
        var expirationTime = (DateTimeOffset)AppSettings.GetInstance.CurrentUser.AccessToken.ExpirationDate.Value.AddSeconds(30);
        TimeSpan timeRemaining = expirationTime - DateTimeOffset.UtcNow;

        if (timeRemaining.TotalSeconds < 30)
        {
            await ApiRepository.RefreshTokenAsync(AppSettings.GetInstance.CurrentUser.RefreshToken);
        }
    }

}
