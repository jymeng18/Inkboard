using Inkboard.API;
using Inkboard.Application;
using Inkboard.Infra;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfraServices(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddScoped<TokenGenerator>();
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Routers
app.MapAuthController();
app.MapUserController();

app.Run();
