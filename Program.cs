using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OAuthAuthorization.Domain.Models;
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
services.AddAuthentication(options =>
 {
     options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
     options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
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
})

.AddCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(1);
})
;

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
