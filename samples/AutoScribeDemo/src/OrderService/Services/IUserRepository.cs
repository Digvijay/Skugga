namespace OrderService.Services;

using OrderService.Models;

public interface IUserRepository
{
    Task<User> GetUserAsync(int userId);
    Task<bool> UserExistsAsync(int userId);
}
