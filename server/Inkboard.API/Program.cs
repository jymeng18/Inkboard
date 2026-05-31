using Inkboard.API;
using Inkboard.Application;
using Inkboard.Domain;
using Inkboard.Infra;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfraServices(builder.Configuration, builder.Environment);
builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddScoped<TokenGenerator>();
builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddScoped<IUserRepository, UserRepository>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Routers
app.MapAuthEndpoint();
app.MapUserEndpoint();

app.Run();
