using FluentValidation;
using Inkboard.Application;
using Inkboard.Application.Auth.DTO;
using Inkboard.Application.Auth.Handlers;
using Inkboard.Application.Interfaces;
using Inkboard.Application.Services;
using Inkboard.Domain.Repositories;
using Inkboard.Infra.Db;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Inkboard.Tests;

public abstract class TestBase
{
    protected static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static int _tokenCounter;

    protected static Mock<ITokenGenerator> CreateTokenGeneratorMock()
    {
        var mock = new Mock<ITokenGenerator>(MockBehavior.Strict);
        mock.Setup(t => t.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(
                (Guid userId, string _, string _) =>
                    $"test-access-token-{userId}-{Interlocked.Increment(ref _tokenCounter)}"
            );
        mock.Setup(t => t.GenerateRefreshToken())
            .Returns(() => $"test-refresh-token-{Guid.NewGuid()}");
        return mock;
    }

    protected static IValidator<RegisterRequestModel> CreateRegisterValidator()
    {
        return new RegisterRequestValidator();
    }

    protected static IValidator<LoginRequestModel> CreateLoginValidator()
    {
        return new LoginRequestValidator();
    }

    protected static AuthService CreateAuthService(
        AppDbContext context,
        Mock<ITokenGenerator>? tokenGenMock = null,
        IValidator<RegisterRequestModel>? registerValidator = null,
        IValidator<LoginRequestModel>? loginValidator = null
    )
    {
        tokenGenMock ??= CreateTokenGeneratorMock();
        registerValidator ??= CreateRegisterValidator();
        loginValidator ??= CreateLoginValidator();

        IUserRepository userRepo = new UserRepository(context);
        ITokenRepository tokenRepo = new TokenRepository(context);

        return new AuthService(
            tokenGenMock.Object,
            userRepo,
            registerValidator,
            loginValidator,
            tokenRepo
        );
    }

    protected const string ValidEmail = "test@example.com";
    protected const string ValidPassword = "password123";
    protected const string ValidUserName = "testuser";
}
