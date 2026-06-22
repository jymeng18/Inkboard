#nullable enable

using Inkboard.Domain.Models;

namespace Inkboard.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    Task CreateUserAsync(User user);
    Task<User?> GetByIdAsync(Guid id);
}
