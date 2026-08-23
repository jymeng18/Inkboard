using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using Azure.Identity;
using Inkboard.Infra.Db;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
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
        if (env.IsDevelopment() || env.IsEnvironment("Testing"))
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("InkboardDb")
            );
        }
        else
        {
            var connectionString = config.GetConnectionString("WebApiDatabase");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'WebApiDatabase' not found."
                );
            }

            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        }

        return services;
    }

    public static IServiceCollection AddAuthenticationServices(
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
                        Encoding.UTF8.GetBytes(config["JwtConfig:Jwt:Key"]!)
                    ),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        var path = context.HttpContext.Request.Path;
                        if (
                            !string.IsNullOrEmpty(accessToken) && (path.StartsWithSegments("/hubs"))
                        )
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                };
            });

        // dont ask questions, just know this is always needed
        return services;
    }

    public static IServiceCollection AddAzureServices(
        this IServiceCollection services,
        IConfiguration config
    )
    {
        string blobUriString =
            config["AzureStorage:BlobUri"]
            ?? throw new InvalidOperationException("BlobUri configuration is missing.");

        services.AddAzureClients(clientBuilder =>
        {
            clientBuilder.AddBlobServiceClient(new Uri(blobUriString));
            DefaultAzureCredential credential = new();
            clientBuilder.UseCredential(credential);
        });

        return services;
    }

    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Auth eps
            options.AddPolicy(
                policyName: "AuthPolicy",
                httpContext =>
                {
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString()
                            ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 15,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                        }
                    );
                }
            );

            // Amount of canvas strokes inserted to db -  upper bounded
            options.AddPolicy(
                policyName: "CanvasOpsPolicy",
                httpContext =>
                {
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString()
                            ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 60,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 15,
                        }
                    );
                }
            );

            // General endpoints
            options.AddPolicy(
                policyName: "GeneralPolicy",
                httpContext =>
                {
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString()
                            ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 30,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 5,
                        }
                    );
                }
            );
        });

        return services;
    }

    public static IServiceCollection AddRedisServices(this IServiceCollection services)
    {
        // TODO: Add Redis service/setup here
        return null!;
    }
}
