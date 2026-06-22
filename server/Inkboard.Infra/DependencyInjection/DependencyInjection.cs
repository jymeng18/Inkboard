using System.Text;
using Inkboard.Infra.Db;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Inkboard.Infra.DependencyInjection;

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


    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration config
    )
    {
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = config["JwtConfig:Issuer"],
                    ValidAudience = config["JwtConfig:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(config["JwtConfig:Jwt:Key"])
                    ),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                };
            });

        // dont ask questions, just know this is always needed
        return services;
    }
}
