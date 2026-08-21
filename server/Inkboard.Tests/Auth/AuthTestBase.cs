using Inkboard.Application.Auth.DTO;
using Inkboard.Application.Auth.Handlers;
using Inkboard.Application.Interfaces;
using Inkboard.Application.Services;
using Inkboard.Infra.Db;
using Moq;

namespace Inkboard.Tests.Auth;


public abstract class AuthTestBase : TestBase
{
    protected const string ValidEmail = "test@example.com";
    protected const string ValidPassword = "password123";
    protected const string ValidUserName = "testuser";

    protected AuthService Service { get; private set; } = null!;

    [TestInitialize]
    public void InitAuthService()
    {
        Service = CreateAuthService(Context);
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

    protected static AuthService CreateAuthService(
        AppDbContext context,
        Mock<ITokenGenerator>? tokenGenMock = null
    )
    {
        tokenGenMock ??= CreateTokenGeneratorMock();
        return new AuthService(
            tokenGenMock.Object,
            new UserRepository(context),
            new RegisterRequestValidator(),
            new LoginRequestValidator(),
            new TokenRepository(context)
        );
    }

    protected static RegisterRequestModel NewRegisterRequest(
        string? userName = null,
        string? email = null,
        string? password = null
    ) =>
        new()
        {
            UserName = userName ?? ValidUserName,
            Email = email ?? ValidEmail,
            Password = password ?? ValidPassword,
        };

    protected Task<RegisterResult> RegisterDefaultAsync() =>
        Service.RegisterAsync(NewRegisterRequest());

    protected Task<LoginResult> LoginDefaultAsync() =>
        Service.LoginAsync(new LoginRequestModel { Email = ValidEmail, Password = ValidPassword });
}
