using FluentValidation;
using Inkboard.API;
using Inkboard.API.Exceptions;
using Inkboard.API.Hubs;
using Inkboard.API.Realtime;
using Inkboard.API.Routes;
using Inkboard.Application.Auth.Handlers;
using Inkboard.Application.Interfaces;
using Inkboard.Application.Services;
using Inkboard.Domain.Repositories;
using Inkboard.Infra.Auth;
using Inkboard.Infra.Azure;
using Inkboard.Infra.BgService;
using Inkboard.Infra.Db;
using Inkboard.Infra.DependencyInjection;
using Inkboard.Infra.Imaging;
using Microsoft.AspNetCore.Http.Features;
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

builder.Services.AddRateLimiting();

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 47_185_920; // Lmit body payload to 45 MB
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52_428_800; // Limit form files to 50 MB
});

if (builder.Environment.IsDevelopment() || builder.Environment.IsProduction())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Services.AddInfraServices(builder.Configuration, builder.Environment);
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddAzureServices(builder.Configuration);

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
builder.Services.AddScoped<IOperationRepository, OperationRepository>();
builder.Services.AddSingleton<OperationWriteQueue>();
builder.Services.AddSingleton<IOperationWriteQueue>(sp => sp.GetRequiredService<OperationWriteQueue>());
builder.Services.AddHostedService<OperationPersistenceWorker>();
builder.Services.AddScoped<IFriendRequestRepository, FriendRequestRepository>();
builder.Services.AddScoped<IFriendshipRepository, FriendshipRepository>();
builder.Services.AddScoped<IFriendsListService, FriendsListService>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddSingleton<IImageValidator, SkiaSharpValidator>();
builder.Services.AddScoped<ICanvasNotifier, CanvasNotifier>();
builder.Services.AddSingleton<IConnectionStore, ConnectionStore>();

builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

builder.Services.AddExceptionHandler<DbExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddExceptionHandler<TimedOutExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var blobService = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();
    await blobService.CreateBlobContainerAsync();
}

app.UseExceptionHandler();
app.UseCors("CorsPolicy");
app.UseAuthentication();

if (!app.Environment.IsEnvironment("Testing"))
    app.UseRateLimiter();

app.UseAuthorization();


// Routers
app.MapAuthEndpoint();
app.MapUserEndpoint();
app.MapPartyEndpoint();
app.MapCanvasEndpoint();
app.MapFriendEndpoint();

app.MapHub<PartyHub>("/hubs/party");
app.MapHub<CanvasHub>("/hubs/canvas");

app.Run();
