using FluentValidation;
using Inkboard.API;
using Inkboard.Application;
using Inkboard.Infra.DependencyInjection;
using Inkboard.API.Routes;
using Inkboard.Application.Interfaces;
using Inkboard.Infra.Auth;
using Inkboard.Domain.Repositories;
using Inkboard.Infra.Db;
using Inkboard.Application.Services;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfraServices(builder.Configuration, builder.Environment);
builder.Services.AddApiServices(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddAuthorization();

builder.Services.AddScoped<ITokenGenerator, TokenGenerator>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenRepository, TokenRepository>();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Routers
app.MapAuthEndpoint();
app.MapUserEndpoint();

app.Run();
