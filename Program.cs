using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OAuthAuthorization.Domain.Models;
using OAuthAuthorizationWebAPI.Helpers;
using OAuthAuthorizationWebAPI.Helpers.Middleware;
using OAuthAuthorizationWebAPI.Persistence;
using OpenIddict.Abstractions;
using System.Text;

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
services.Configure<OAuthAuthorizationWebAPI.Helpers.Options.JwtBearerOptions>(configuration.GetSection("JwtBearerOptions"));
var jwtOptions = configuration.GetSection("JwtBearerOptions").Get<OAuthAuthorizationWebAPI.Helpers.Options.JwtBearerOptions>();


#endregion

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
               //options.AddSigningKey(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtOptions.TokenValidationParameters.IssuerSigningKeyString)));
               

           })
           .AddValidation(options =>
           {
               //options.EnableAuthorizationEntryValidation();
               //options.UseSystemNetHttp();
               //options.SetIssuer("https://localhost:7292/");
               options.UseLocalServer();
               options.UseAspNetCore();
           })
           
           ;


services.AddAuthentication(options =>
{
    options.DefaultScheme = OpenIddictConstants.Schemes.Bearer;
    //options.DefaultAuthenticateScheme = OpenIddictConstants.Schemes.Bearer;//JwtBearerDefaults.AuthenticationScheme;
})
//.AddJwtBearer(options =>
//{
//    options.Authority = jwtOptions.Authority;
//    options.ClaimsIssuer = jwtOptions.ClaimsIssuer;
//    options.Audience = jwtOptions.Audience;
//    options.RequireHttpsMetadata = jwtOptions.RequireHttpsMetadata;

//    options.TokenValidationParameters = jwtOptions.TokenValidationParameters;
//    options.TokenValidationParameters.ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 };
//    options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtOptions.TokenValidationParameters.IssuerSigningKeyString));
//})
.AddBearerToken(options =>
{
    
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
//services.AddHttpContextAccessor();



var app = builder.Build();

app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AddTokenToRequestMiddleware>();

app.UseEndpoints(options =>
{
    options.MapControllers();
    options.MapDefaultControllerRoute();
});

app.MapControllers();

app.Run();