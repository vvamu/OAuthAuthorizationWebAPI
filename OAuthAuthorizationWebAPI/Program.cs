using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OAuthAuthorization.Domain.Models;
using OAuthAuthorizationWebAPI.Application;
using OAuthAuthorizationWebAPI.Helpers.Middleware;
using OAuthAuthorizationWebAPI.Persistence;
using OpenIddict.Abstractions;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddControllers();
services.AddEndpointsApiExplorer();

services.AddTransient<IApplicationUserService, ApplicationUserService>();

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
               options.SetTokenEndpointUris("/api/client/token");

               options.AllowPasswordFlow()
                      .AllowRefreshTokenFlow()
                      ;

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
               options.UseLocalServer();
               options.UseAspNetCore();
           })

           ;


services.AddAuthentication(options =>
{
    options.DefaultScheme = OpenIddictConstants.Schemes.Bearer;
});

services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

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
app.UseMiddleware<AddTokenToRequestMiddleware>();

app.Run();