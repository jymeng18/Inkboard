#nullable enable

namespace Inkboard.Domain;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    Task CreateUserAsync(User user);
    Task<User?> GetByIdAsync(Guid id);
}
