using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using OpenIddict.Abstractions;
using OpenIddict.Client;
using Polly;

namespace OAuthAuthorization.Testing.Configurations;






public class WebApiConfiguration<TStartup> : IDisposable where TStartup : class
{
    private WebApplication _app = null!;
    //private DataContext _context = null!;
    private HttpClient _client = null!;
    private readonly TestServer _server;
    //private IDataClient _refitClient = null!;
    private IServiceScope _scope = null!;

    WebApiConfiguration()
    {
        //var builder = WebApplication.CreateBuilder().ConfigureServices();
        //_app = builder.CreateApplication();
        //_app.Urls.Add("http://*:8080");
        //await _app.StartAsync();
        //_scope = _app.Services.CreateScope();
        //_context = _scope.ServiceProvider.GetRequiredService<DataContext>();
        //_client = new HttpClient { BaseAddress = new Uri("http://localhost:8080") };
        //_refitClient = RestService.For<IDataClient>(_client);


        var builder = new WebHostBuilder().UseStartup<TStartup>()
        .ConfigureServices(services =>
        {
            services.AddOpenIddict()

            .AddServer(options =>
            {
                options.SetTokenEndpointUris("/api/client/token")
                ;

                options
                        .AllowPasswordFlow()
                        .AllowRefreshTokenFlow();

                options.AddDevelopmentEncryptionCertificate()
                        .AddDevelopmentSigningCertificate();

                options.UseAspNetCore()
                        .EnableTokenEndpointPassthrough()
                        .EnableAuthorizationEndpointPassthrough()
                        .EnableLogoutEndpointPassthrough();

                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(1));
                options.SetRefreshTokenLifetime(TimeSpan.FromDays(30));

            })
            .AddValidation(options =>
            {
                options.UseLocalServer(); //OpenIddict.Validation.ServerIntegration
                options.UseAspNetCore();
            })
            ;

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = OpenIddictConstants.Schemes.Bearer;
            });
            var provider = services.BuildServiceProvider();
        })
        .Configure(app =>
        {
            app.Run(async context =>
            {
                await context.Response.WriteAsync("Test response");
            });
        });
        _server = new TestServer(builder);

        _client = _server.CreateClient();
        _client.BaseAddress = new Uri("http://localhost:5000");

    }

    static async Task<IHost> CreateClientHost()
    {
        var builder = new ConfigurationBuilder()
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
                     .SetProductInformation(typeof(Program).Assembly); ; //Program - TestHost
                })
                ;
            var provider = services.BuildServiceProvider();
        })

        .Build();
        return host;


    }

    static async Task<IHost> CreateServerHost()
    {
        var host = new HostBuilder()
        .ConfigureServices(services =>
        {
            services.AddOpenIddict()

           .AddServer(options =>
           {
               options.SetTokenEndpointUris("/api/client/token")
               ;

               options
                      .AllowPasswordFlow()
                      .AllowRefreshTokenFlow();

               options.AddDevelopmentEncryptionCertificate()
                      .AddDevelopmentSigningCertificate();

               options.UseAspNetCore()
                      .EnableTokenEndpointPassthrough()
                      .EnableAuthorizationEndpointPassthrough()
                      .EnableLogoutEndpointPassthrough()
                      ;

               options.SetAccessTokenLifetime(TimeSpan.FromMinutes(1));
               options.SetRefreshTokenLifetime(TimeSpan.FromDays(30));

           })
           .AddValidation(options =>
           {
               options.UseLocalServer(); //OpenIddict.Validation.ServerIntegration
               options.UseAspNetCore();
           })
           ;

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = OpenIddictConstants.Schemes.Bearer;
            });
            var provider = services.BuildServiceProvider();

        })

        .Build();
        return host;

    }

    public void Dispose()
    {
        _client.Dispose();
        _server.Dispose();
    }
}