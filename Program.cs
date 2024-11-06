using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OAuthAuthorization.Domain.Models;
using OAuthAuthorizationWebAPI.Persistence;
using OpenIddict.Abstractions;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

#region Options pattern
IConfiguration configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json")
        .Build();

services.AddSingleton(configuration);
#endregion

services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
        options.UseOpenIddict();
    }
);
services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

services.AddOpenIddict()
           .AddCore(options =>
           {
               options.UseEntityFrameworkCore()
                      .UseDbContext<ApplicationDbContext>();
           })
           .AddServer(options =>
           {
               options.SetAuthorizationEndpointUris("/api/authorize")
                      .SetTokenEndpointUris("/api/token")
                      .SetLogoutEndpointUris("/api/logout")
                      ;

               options.AllowClientCredentialsFlow()
                      .AllowPasswordFlow()
                      .AllowRefreshTokenFlow();
                      //.AllowAuthorizationCodeFlow()
                      //.AllowImplicitFlow()
                      //.AllowHybridFlow()

               options.AddDevelopmentEncryptionCertificate()
                      .AddDevelopmentSigningCertificate();

               options.UseAspNetCore()
                      .EnableTokenEndpointPassthrough()
                      .EnableAuthorizationEndpointPassthrough()
                      .EnableLogoutEndpointPassthrough();

               //options.RegisterClaims(configuration.GetSection("OpenIddict:Claims").Get<string[]>()!); // Expose all the supported claims in the discovery document.
               //options.RegisterScopes(configuration.GetSection("OpenIddict:Scopes").Get<string[]>()!); // Expose all the supported scopes in the discovery document.

               // Note: an ephemeral signing key is deliberately used to make the "OP-Rotation-OP-Sig"
               // test easier to run as restarting the application is enough to rotate the keys.
               //options.AddEphemeralEncryptionKey()
               //       .AddEphemeralSigningKey();

               // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
               //
               // Note: the pass-through mode is not enabled for the token endpoint
               // so that token requests are automatically handled by OpenIddict.
               //options.UseAspNetCore()
               //       .EnableAuthorizationEndpointPassthrough()
               //       .EnableAuthorizationRequestCaching()
               //       .EnableLogoutEndpointPassthrough();

           })
           
           .AddValidation(options =>
           {
               options.UseAspNetCore();        
               options.EnableAuthorizationEntryValidation();

               options.SetIssuer("https://localhost:7292/");
               options.SetConfiguration(new OpenIddictConfiguration() { });

           })
           ;


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

app.UseDeveloperExceptionPage();
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseEndpoints(options =>
{
    options.MapControllers();
    options.MapDefaultControllerRoute();
});

app.MapControllers();

app.Run();
