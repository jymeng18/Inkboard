using Inkboard.Application.Auth.DTO;

namespace Inkboard.Tests.Auth;

/*
 * Boundary coverage for the length limits on register and login. The caps
 * exist so an oversized field is rejected before it reaches the database or
 * the password hasher, so these check the exact edges (max and max + 1) rather
 * than a single "too long" value, and that rejection happens early.
 */
[TestClass]
public sealed class AuthLimitTests : TestBase
{
    private const int UserNameMin = 3;
    private const int UserNameMax = 30;
    private const int PasswordMin = 6;
    private const int PasswordMax = 128;
    private const int EmailMax = 256;

    /* "@example.com" is 12 characters, so the local part carries the rest. */
    private static string EmailOfLength(int length) =>
        new string('a', length - "@example.com".Length) + "@example.com";

    private static RegisterRequestModel Register(
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

    // ---------- Register: user name ----------

    [TestMethod]
    public async Task Register_UserNameAtMinLength_Succeeds()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var result = await service.RegisterAsync(
            Register(userName: new string('a', UserNameMin))
        );

        Assert.IsTrue(result.Success);
        Assert.IsNull(result.ValidationErrors);
    }

    [TestMethod]
    public async Task Register_UserNameAtMaxLength_Succeeds()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var result = await service.RegisterAsync(
            Register(userName: new string('a', UserNameMax))
        );

        Assert.IsTrue(result.Success);
        Assert.IsNull(result.ValidationErrors);
    }

    [TestMethod]
    public async Task Register_UserNameOneOverMax_ReturnsValidationError()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var result = await service.RegisterAsync(
            Register(userName: new string('a', UserNameMax + 1))
        );

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ValidationErrors);
        Assert.IsTrue(result.ValidationErrors.ContainsKey("UserName"));
        Assert.Contains("not exceed 30", result.ValidationErrors["UserName"][0]);
    }

    // ---------- Register: password ----------

    [TestMethod]
    public async Task Register_PasswordAtMinLength_Succeeds()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var result = await service.RegisterAsync(
            Register(password: new string('p', PasswordMin))
        );

        Assert.IsTrue(result.Success);
        Assert.IsNull(result.ValidationErrors);
    }

    [TestMethod]
    public async Task Register_PasswordAtMaxLength_Succeeds()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var result = await service.RegisterAsync(
            Register(password: new string('p', PasswordMax))
        );

        Assert.IsTrue(result.Success);
        Assert.IsNull(result.ValidationErrors);
    }

    [TestMethod]
    public async Task Register_PasswordOneOverMax_ReturnsValidationError()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var result = await service.RegisterAsync(
            Register(password: new string('p', PasswordMax + 1))
        );

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ValidationErrors);
        Assert.IsTrue(result.ValidationErrors.ContainsKey("Password"));
        Assert.Contains("not exceed 128", result.ValidationErrors["Password"][0]);
    }

    /*
     * The reason the cap exists: a huge password must be turned away by the
     * validator, never handed to BCrypt, and never written to the database.
     */
    [TestMethod]
    public async Task Register_HugePassword_IsRejectedAndPersistsNothing()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var result = await service.RegisterAsync(
            Register(password: new string('p', 100_000))
        );

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ValidationErrors);
        Assert.IsTrue(result.ValidationErrors.ContainsKey("Password"));
        Assert.IsNull(result.UserId);
        Assert.AreEqual(0, context.Users.Count());
    }

    // ---------- Register: email ----------

    [TestMethod]
    public async Task Register_EmailAtMaxLength_Succeeds()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var email = EmailOfLength(EmailMax);
        Assert.AreEqual(EmailMax, email.Length);

        var result = await service.RegisterAsync(Register(email: email));

        Assert.IsTrue(result.Success);
        Assert.IsNull(result.ValidationErrors);
    }

    [TestMethod]
    public async Task Register_EmailOneOverMax_ReturnsValidationError()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var result = await service.RegisterAsync(
            Register(email: EmailOfLength(EmailMax + 1))
        );

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ValidationErrors);
        Assert.IsTrue(result.ValidationErrors.ContainsKey("Email"));
        Assert.Contains("not exceed 256", result.ValidationErrors["Email"][0]);
    }

    [TestMethod]
    public async Task Register_HugeEmail_IsRejectedAndPersistsNothing()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var result = await service.RegisterAsync(
            Register(email: EmailOfLength(100_000))
        );

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ValidationErrors);
        Assert.IsTrue(result.ValidationErrors.ContainsKey("Email"));
        Assert.AreEqual(0, context.Users.Count());
    }

    // ---------- Login ----------

    /*
     * Login is unauthenticated, so its caps are the ones that matter most.
     * A validation failure has to come back as ValidationErrors with no
     * ErrorMessage — if the oversized value had made it past the validator,
     * the lookup below it would have produced "Invalid email or password."
     */
    [TestMethod]
    public async Task Login_PasswordOneOverMax_IsRejectedBeforeLookup()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var result = await service.LoginAsync(
            new LoginRequestModel
            {
                Email = "nobody@example.com",
                Password = new string('p', PasswordMax + 1),
            }
        );

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ValidationErrors);
        Assert.IsTrue(result.ValidationErrors.ContainsKey("Password"));
        Assert.IsNull(result.ErrorMessage);
    }

    [TestMethod]
    public async Task Login_HugePassword_IsRejectedBeforeHashing()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        await service.RegisterAsync(Register());

        var result = await service.LoginAsync(
            new LoginRequestModel { Email = ValidEmail, Password = new string('p', 100_000) }
        );

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ValidationErrors);
        Assert.IsTrue(result.ValidationErrors.ContainsKey("Password"));
        Assert.IsNull(result.AccessToken);
    }

    /*
     * A 128-character password is a legitimate one, so it has to reach the
     * hasher and fail there as an ordinary credential mismatch — not get
     * turned away as malformed input.
     */
    [TestMethod]
    public async Task Login_PasswordAtMaxLength_ReachesCredentialCheck()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        await service.RegisterAsync(Register());

        var result = await service.LoginAsync(
            new LoginRequestModel { Email = ValidEmail, Password = new string('p', PasswordMax) }
        );

        Assert.IsFalse(result.Success);
        Assert.IsNull(result.ValidationErrors);
        Assert.AreEqual("Invalid email or password.", result.ErrorMessage);
    }

    [TestMethod]
    public async Task Login_PasswordAtMaxLength_SucceedsForMatchingAccount()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var password = new string('p', PasswordMax);
        await service.RegisterAsync(Register(password: password));

        var result = await service.LoginAsync(
            new LoginRequestModel { Email = ValidEmail, Password = password }
        );

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.AccessToken);
    }

    [TestMethod]
    public async Task Login_EmailOneOverMax_ReturnsValidationError()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var result = await service.LoginAsync(
            new LoginRequestModel
            {
                Email = EmailOfLength(EmailMax + 1),
                Password = ValidPassword,
            }
        );

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ValidationErrors);
        Assert.IsTrue(result.ValidationErrors.ContainsKey("Email"));
    }

    [TestMethod]
    public async Task Login_EmptyPassword_ReturnsValidationError()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var result = await service.LoginAsync(
            new LoginRequestModel { Email = ValidEmail, Password = "" }
        );

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ValidationErrors);
        Assert.IsTrue(result.ValidationErrors.ContainsKey("Password"));
    }

    [TestMethod]
    public async Task Login_EmptyEmail_ReturnsValidationError()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var result = await service.LoginAsync(
            new LoginRequestModel { Email = "", Password = ValidPassword }
        );

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ValidationErrors);
        Assert.IsTrue(result.ValidationErrors.ContainsKey("Email"));
    }

    [TestMethod]
    public async Task Login_MalformedEmail_ReturnsValidationError()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var result = await service.LoginAsync(
            new LoginRequestModel { Email = "not-an-email", Password = ValidPassword }
        );

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ValidationErrors);
        Assert.IsTrue(result.ValidationErrors.ContainsKey("Email"));
    }
}
