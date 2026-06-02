using FluentValidation;
using Inkboard.Application.Auth.DTO;
using Inkboard.Application.Interfaces;
using Inkboard.Domain.Models;
using Inkboard.Domain.Repositories;

namespace Inkboard.Application.Services;

public class AuthService : IAuthService
{
  private readonly ITokenGenerator _tokenGenerator;
  private readonly IUserRepository _repository;
  private readonly IValidator<RegisterRequestModel> _validator;

  public AuthService(ITokenGenerator tokenGenerator, IUserRepository repository, IValidator<RegisterRequestModel> validator)
  {
    _tokenGenerator = tokenGenerator;
    _repository = repository;
    _validator = validator;
  }

  public async Task<LoginResult> LoginAsync(LoginRequestModel request)
  {
        var user = await _repository.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return new LoginResult { ErrorMessage = "Invalid email or password." };
        }
        
        // compare hashpw <-> pw
        bool validPassword = BCrypt.Net.BCrypt.EnhancedVerify(request.Password, user.PasswordHash);
        if (!validPassword)
        {
            return new LoginResult { ErrorMessage = "Invalid email or password."};
        }

        var token = _tokenGenerator.GenerateToken(user.Id, request.Email);
        return new LoginResult { Success = true, AccessToken = token };
  }

  public async Task<RegisterResult> RegisterAsync(RegisterRequestModel request)
  {
        bool emailExists = await _repository.EmailExistsAsync(request.Email);
        if (emailExists)
        {
            return new RegisterResult { ErrorMessage = "Email is already registered."};
        }

        // Authorize all fields in request
        var result = await _validator.ValidateAsync(request);
        if (!result.IsValid)
        {
            var errors = result
                .Errors.GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return new RegisterResult { ValidationErrors = errors };
        }

        // Create new user to save to db(Users)
        User user = new User
        {
            UserName = request.UserName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(request.Password, 12), // workfactor = 12
        };

        try
        {
            await _repository.CreateUserAsync(user);
            return new RegisterResult { Success = true, UserId = user.Id }; 
        }
        catch (Exception)
        {
            return new RegisterResult { ErrorMessage = "An unexpected error occured."};
        }
    }
  }