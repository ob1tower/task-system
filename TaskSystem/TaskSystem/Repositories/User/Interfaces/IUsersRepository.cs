using TaskSystem.Entities;
using TaskSystem.Models;

namespace TaskSystem.Repositories.Interfaces;

public interface IUsersRepository
{
    Task CreateUsers(User user);
    Task<UserEntity?> GetByEmail(string email);
    Task<UserEntity?> GetByUserName(string userName);
}