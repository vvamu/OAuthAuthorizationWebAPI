using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using OAuthAuthorization.Testing.Persistence;

namespace OAuthAuthorization.Testing.Configurations;

public class CustomAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Удалим зарегистрированный DataContext
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Зарегистрируем снова с указанием на тестовую БД
            services.AddDbContextPool<ApplicationDbContext>(opts => opts.UseSqlite("Host=localhost;Database=test_ci_db;Username=postgres;Password=;"));

            // Обеспечим создание БД
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var context = scopedServices.GetRequiredService<ApplicationDbContext>();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            // Здесь можно выполнить код "наполняющий" БД тестовыми данными...
        });
    }
}
