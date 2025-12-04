using Microsoft.EntityFrameworkCore;
using TaskSystem.DataAccess;
using TaskSystem.Entities;
using TaskSystem.Models;
using TaskSystem.Repositories.Interfaces;

namespace TaskSystem.Repositories;

public class UsersRepository : IUsersRepository
{
    private readonly UserDbContext _context;

    public UsersRepository(UserDbContext context)
    {
        _context = context;
    }

    public async Task CreateUsers(User user)
    {
        var userEntities = new UserEntity
        {
            UserId = user.UserId,
            UserName = user.UserName,
            PasswordHash = user.PasswordHash,
            Email = user.Email,
            CreatedAt = DateTime.UtcNow,
            RoleId = 1
        };

        await _context.Users.AddAsync(userEntities);
        await _context.SaveChangesAsync();
    }

    public async Task<UserEntity?> GetByEmail(string email)
    {
        return await _context.Users
            .AsTracking()
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<UserEntity?> GetByUserName(string userName)
    {
        return await _context.Users
            .AsTracking()
            .FirstOrDefaultAsync(u => u.UserName == userName);
    }
}
