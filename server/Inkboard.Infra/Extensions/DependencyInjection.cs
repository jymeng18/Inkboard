using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inkboard.Infra;

public static class DependencyInjection
{
    public static IServiceCollection AddInfraServices(
        this IServiceCollection services,
        IConfiguration config
    )
    {
        var connectionString = config.GetConnectionString("WebApiDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'WebApiDatabase' not found.");
        }

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
