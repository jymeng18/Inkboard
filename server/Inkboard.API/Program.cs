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


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfraServices(builder.Configuration, builder.Environment);
builder.Services.AddApiServices(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddAuthorization();
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

builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Routers
app.MapAuthEndpoint();
app.MapUserEndpoint();
app.MapPartyEndpoint();

app.MapHub<PartyHub>("hubs/party");

app.Run();
