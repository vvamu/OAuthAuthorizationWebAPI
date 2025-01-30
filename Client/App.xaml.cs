using Client.ViewModels;
using Client.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Client;
using System.Reflection;
using System.Windows;

namespace Client;

public partial class App : PrismApplicationBase
{
    static App()
    {
        var res = RunApp();

    }
    static async Task RunApp()
    {
        try
        {
            var host = await CreateHost();
            ApiRepository.Provider = host.Services;

            await host.RunAsync();

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
    protected override Window CreateShell()
    {
        var mainWindowUri = new Uri("/Client;component/Views/MainWindow.xaml", UriKind.Relative);
        return Container.Resolve<MainWindow>();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterForNavigation<Client.Views.MainWindow, MainWindowViewModel>("MainWindow");
        containerRegistry.RegisterForNavigation<Client.Views.MainPage, MainPageViewModel>("MainPage");
        containerRegistry.RegisterForNavigation<Client.Views.UsersPage, UsersPageViewModel>("UsersPage");
        containerRegistry.RegisterForNavigation<Client.Views.RegistrationPage, RegistrationPageViewModel>("RegistrationPage");
        containerRegistry.RegisterForNavigation<Client.Views.SignInPage, SignInPageViewModel>("SignInPage");
    }

    protected override void ConfigureViewModelLocator()
    {
        base.ConfigureViewModelLocator();

        ViewModelLocationProvider.SetDefaultViewTypeToViewModelTypeResolver((viewType) =>
        {
            var viewName = viewType.FullName.Replace(".ViewModels.", ".CustomNamespace.");
            var viewAssemblyName = viewType.GetTypeInfo().Assembly.FullName;
            var viewModelName = $"{viewName}ViewModel, {viewAssemblyName}";
            return Type.GetType(viewModelName);
        });

        ViewModelLocationProvider.Register<MainWindow, MainWindowViewModel>();
    }

    protected override IContainerExtension CreateContainerExtension()
    {
        return new UnityContainerExtension();
    }

    static async Task<IHost> CreateHost()
    {
        var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        var configuration = builder.Build();

        var issuer = new Uri(configuration.GetValue<string>("OpenIddictClientOptions:UrlIssuer"), UriKind.Absolute);
        var clientId = configuration.GetValue<string>("OpenIddictClientOptions:ClientId");
        var clientSecret = configuration.GetValue<string>("OpenIddictClientOptions:ClientSecret");


        var host = new HostBuilder()
        .ConfigureServices(services =>
        {
            services.AddOpenIddict()
                .AddClient(options =>
                {
                    options.AllowPasswordFlow()
                           .AllowRefreshTokenFlow();

                    options.DisableTokenStorage();


                    options.AddDevelopmentEncryptionCertificate()
                           .AddDevelopmentSigningCertificate();

                    options.AddRegistration(new OpenIddictClientRegistration
                    {
                        Issuer = issuer,
                        ClientId = clientId,
                        ClientSecret = clientSecret,
                    });
                    options.UseSystemNetHttp()
                     .SetProductInformation(typeof(App).Assembly); ;
                })
                ;
            var provider = services.BuildServiceProvider();
        })

        .Build();

        await ApiRepository.CreateConnectClientToServer(issuer, clientId, clientSecret);


        return host;
    }
}