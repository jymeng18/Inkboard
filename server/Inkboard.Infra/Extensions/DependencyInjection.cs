using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Inkboard.Infra;

public static class DependencyInjection
{
    public static IServiceCollection AddInfraServices(
        this IServiceCollection services,
        IConfiguration config,
        IHostEnvironment env
    )
    {
        if (env.IsDevelopment())
        {
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("InkboardDb"));
        }
        else
        {
            var connectionString = config.GetConnectionString("WebApiDatabase");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Connection string 'WebApiDatabase' not found.");
            }

            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        }

        return services;
    }
}
