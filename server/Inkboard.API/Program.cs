using FluentValidation;
using Inkboard.API;
using Inkboard.API.Hubs;
using Inkboard.API.Realtime;
using Inkboard.API.Routes;
using Inkboard.Application;
using Inkboard.Application.Interfaces;
using Inkboard.Application.Services;
using Inkboard.Domain.Repositories;
using Inkboard.Infra.Auth;
using Inkboard.Infra.Db;
using Inkboard.Infra.DependencyInjection;
using Microsoft.AspNetCore.SignalR;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddInfraServices(builder.Configuration, builder.Environment);
builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddAzureServices(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IUserIdProvider, SubClaimUserIdProvider>();
builder.Services.AddSignalR();


builder.Services.AddScoped<ITokenGenerator, TokenGenerator>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenRepository, TokenRepository>();
builder.Services.AddScoped<IPartyRepository, PartyRepository>();
builder.Services.AddScoped<IPartyInviteRepository, PartyInviteRepository>();
builder.Services.AddScoped<IBlockListRepository, BlockListRepository>();
builder.Services.AddScoped<IPartyService, PartyService>();
builder.Services.AddScoped<IPartyNotifier, PartyNotifier>();
builder.Services.AddScoped<ICanvasRepository, CanvasRepository>();
builder.Services.AddScoped<ICanvasService, CanvasService>();

builder.Services.AddSingleton<IConnectionStore, ConnectionStore>();

builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

var app = builder.Build();

app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

// Routers
app.MapAuthEndpoint();
app.MapUserEndpoint();
app.MapPartyEndpoint();
app.MapCanvasEndpoint();

app.MapHub<PartyHub>("/hubs/party");

app.Run();
