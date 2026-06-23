using Inkboard.Application.Auth.DTO;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Tests.Auth;

[TestClass]
public sealed class RegisterTests : TestBase
{
    [TestMethod]
    public async Task ValidRequest_ReturnsSuccessWithUserId()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var result = await service.RegisterAsync(new RegisterRequestModel
        {
            UserName = ValidUserName,
            Email = ValidEmail,
            Password = ValidPassword,
        });

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.UserId);
        Assert.AreNotEqual(Guid.Empty, result.UserId.Value);
    }

    [TestMethod]
    public async Task DuplicateEmail_ReturnsError()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        await service.RegisterAsync(new RegisterRequestModel
        {
            UserName = ValidUserName,
            Email = ValidEmail,
            Password = ValidPassword,
        });

        var result = await service.RegisterAsync(new RegisterRequestModel
        {
            UserName = "otheruser",
            Email = ValidEmail,
            Password = ValidPassword,
        });

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Email is already registered.", result.ErrorMessage);
        Assert.IsNull(result.UserId);
    }

    [TestMethod]
    public async Task DuplicateEmail_ReturnsBeforeValidation()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        await service.RegisterAsync(new RegisterRequestModel
        {
            UserName = ValidUserName,
            Email = ValidEmail,
            Password = ValidPassword,
        });

        var result = await service.RegisterAsync(new RegisterRequestModel
        {
            UserName = "",
            Email = ValidEmail,
            Password = "ab",
        });

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Email is already registered.", result.ErrorMessage);
        Assert.IsNull(result.ValidationErrors);
    }

    [TestMethod]
    public async Task EmptyUserName_ReturnsValidationError()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var result = await service.RegisterAsync(new RegisterRequestModel
        {
            UserName = "",
            Email = ValidEmail,
            Password = ValidPassword,
        });

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ValidationErrors);
        Assert.IsTrue(result.ValidationErrors.ContainsKey("UserName"));
    }

    [TestMethod]
    public async Task UserNameTooShort_ReturnsValidationError()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var result = await service.RegisterAsync(new RegisterRequestModel
        {
            UserName = "ab",
            Email = ValidEmail,
            Password = ValidPassword,
        });

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ValidationErrors);
        Assert.IsTrue(result.ValidationErrors.ContainsKey("UserName"));
        Assert.Contains("at least 3 characters", result.ValidationErrors["UserName"][0]);
    }

    [TestMethod]
    public async Task UserNameTooLong_ReturnsValidationError()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var result = await service.RegisterAsync(new RegisterRequestModel
        {
            UserName = new string('a', 31),
            Email = ValidEmail,
            Password = ValidPassword,
        });

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ValidationErrors);
        Assert.IsTrue(result.ValidationErrors.ContainsKey("UserName"));
    }

    [TestMethod]
    public async Task EmptyPassword_ReturnsValidationError()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var result = await service.RegisterAsync(new RegisterRequestModel
        {
            UserName = ValidUserName,
            Email = ValidEmail,
            Password = "",
        });

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ValidationErrors);
        Assert.IsTrue(result.ValidationErrors.ContainsKey("Password"));
    }

    [TestMethod]
    public async Task PasswordTooShort_ReturnsValidationError()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var result = await service.RegisterAsync(new RegisterRequestModel
        {
            UserName = ValidUserName,
            Email = ValidEmail,
            Password = "12345",
        });

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ValidationErrors);
        Assert.IsTrue(result.ValidationErrors.ContainsKey("Password"));
        Assert.Contains("at least 6 characters", result.ValidationErrors["Password"][0]);
    }

    [TestMethod]
    public async Task EmptyEmail_ReturnsValidationError()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var result = await service.RegisterAsync(new RegisterRequestModel
        {
            UserName = ValidUserName,
            Email = "",
            Password = ValidPassword,
        });

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ValidationErrors);
        Assert.IsTrue(result.ValidationErrors.ContainsKey("Email"));
    }

    [TestMethod]
    public async Task InvalidEmailFormat_ReturnsValidationError()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var result = await service.RegisterAsync(new RegisterRequestModel
        {
            UserName = ValidUserName,
            Email = "not-an-email",
            Password = ValidPassword,
        });

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ValidationErrors);
        Assert.IsTrue(result.ValidationErrors.ContainsKey("Email"));
    }

    [TestMethod]
    public async Task ValidRequest_PersistsUserToDatabase()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var result = await service.RegisterAsync(new RegisterRequestModel
        {
            UserName = ValidUserName,
            Email = ValidEmail,
            Password = ValidPassword,
        });

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.UserId);

        var savedUser = await context.Users.FindAsync(result.UserId.Value);
        Assert.IsNotNull(savedUser);
        Assert.AreEqual(ValidUserName, savedUser.UserName);
        Assert.AreEqual(ValidEmail, savedUser.Email);
        Assert.IsFalse(string.IsNullOrEmpty(savedUser.PasswordHash));
        Assert.AreNotEqual(ValidPassword, savedUser.PasswordHash);
    }
}
