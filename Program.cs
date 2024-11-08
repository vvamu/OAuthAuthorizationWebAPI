using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OAuthAuthorization.Domain.Models;
using OAuthAuthorizationWebAPI.Helpers.Middleware;
using OAuthAuthorizationWebAPI.Persistence;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using System.Text;
using static OpenIddict.Abstractions.OpenIddictConstants.Permissions;

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
               options.SetAuthorizationEndpointUris("/api/user/authorize")
                      .SetTokenEndpointUris("/api/user/token")
                      .SetLogoutEndpointUris("/api/user/logout")
                      ;

               options
                      .AllowPasswordFlow()
                      .AllowRefreshTokenFlow();
                       //.AllowClientCredentialsFlow()
                       //.AllowAuthorizationCodeFlow()
                       //.AllowImplicitFlow()
                       //.AllowHybridFlow()

               options.AddDevelopmentEncryptionCertificate()
                      .AddDevelopmentSigningCertificate();

               options.UseAspNetCore()
                      .EnableTokenEndpointPassthrough()
                      .EnableAuthorizationEndpointPassthrough()
                      .EnableLogoutEndpointPassthrough();


               options.SetAccessTokenLifetime(TimeSpan.FromMinutes(1));
               options.SetRefreshTokenLifetime(TimeSpan.FromDays(30)); // Установка срока действия refresh-токена
               options.AddSigningKey(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtOptions.TokenValidationParameters.IssuerSigningKeyString)));

           })
           .AddValidation(options =>
           {
               options.UseAspNetCore();        
               options.EnableAuthorizationEntryValidation();

               options.SetIssuer("https://localhost:7292/");
               options.SetConfiguration(new OpenIddictConfiguration() { });
               

           });


services.AddAuthentication(options =>
{
    options.DefaultScheme = OpenIddictConstants.Schemes.Bearer;
    //options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = jwtOptions.Authority;
    options.ClaimsIssuer = jwtOptions.ClaimsIssuer;
    options.Audience = jwtOptions.Audience;
    options.RequireHttpsMetadata = jwtOptions.RequireHttpsMetadata;

    options.TokenValidationParameters = jwtOptions.TokenValidationParameters;
    options.TokenValidationParameters.ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 };
    options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtOptions.TokenValidationParameters.IssuerSigningKeyString));
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
services.AddHttpContextAccessor();



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
app.UseMiddleware<AddTokenToRequestMiddleware>();



app.UseEndpoints(options =>
{
    options.MapControllers();
    options.MapDefaultControllerRoute();
});

app.MapControllers();

app.Run();