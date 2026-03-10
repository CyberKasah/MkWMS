using Microsoft.EntityFrameworkCore;
using MkWMS.API.Services;
using MkWMS.Data.Context;
using MkWMS.Data.Entities;

namespace MkWMS.API.Services;

public class UserRoleService : IUserRoleService
{
    private readonly MkWMSDbContext _context;

    public UserRoleService(MkWMSDbContext context)
    {
        _context = context;
    }

    public async Task<List<string>> GetUserRolesAsync(int userId)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .ToListAsync();
    }

    public async Task<bool> AssignRoleAsync(int userId, int roleId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        // Проверяем существование роли
        var roleExists = await _context.Roles.AnyAsync(r => r.Id == roleId);
        if (!roleExists) return false;

        // Проверяем, есть ли уже такая роль
        var exists = await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (exists) return true; // уже назначена — считаем успехом

        _context.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveRoleAsync(int userId, int roleId)
    {
        var userRole = await _context.UserRoles
            .Include(ur => ur.Role)
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (userRole == null)
            return false;

        var remainingRoles = await _context.UserRoles
            .CountAsync(ur => ur.UserId == userId);

        if (remainingRoles <= 1)
            throw new InvalidOperationException("Нельзя оставить пользователя без ролей.");

        // Защита от удаления последнего администратора
        if (userRole.Role.Name == "Администратор" || userRole.Role.Name == "Admin")
        {
            var adminsCount = await _context.UserRoles
                .Include(ur => ur.Role)
                .CountAsync(ur => ur.Role.Name == "Администратор" || ur.Role.Name == "Admin");

            if (adminsCount <= 1)
                throw new InvalidOperationException("Нельзя удалить последнего администратора.");
        }

        _context.UserRoles.Remove(userRole);
        await _context.SaveChangesAsync();
        return true;
    }

    // Дополнительный метод (не в интерфейсе, но полезный для массового назначения)
    public async Task<bool> AssignRolesAsync(int userId, List<int> roleIds)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        var existingRoles = await _context.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync();

        if (existingRoles.Count != roleIds.Count) return false;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Удаляем все текущие роли
            var currentRoles = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .ToListAsync();
            _context.UserRoles.RemoveRange(currentRoles);

            // Добавляем новые
            var newRoles = roleIds
                .Distinct()
                .Select(roleId => new UserRole { UserId = userId, RoleId = roleId });

            await _context.UserRoles.AddRangeAsync(newRoles);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    // Твой метод RemoveAllRolesAsync (оставляем как есть, он не в интерфейсе)
    public async Task RemoveAllRolesAsync(int userId)
    {
        var roles = await _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == userId)
            .ToListAsync();

        if (!roles.Any()) return;

        // Проверка последнего администратора
        var adminRoleIds = await _context.Roles
            .Where(r => r.Name == "Администратор" || r.Name == "Admin")
            .Select(r => r.Id)
            .ToListAsync();

        var isLastAdmin = roles.Any(r => adminRoleIds.Contains(r.RoleId)) &&
                          await _context.UserRoles
                              .CountAsync(ur => adminRoleIds.Contains(ur.RoleId)) == 1;

        if (isLastAdmin)
            throw new InvalidOperationException("Нельзя удалить последнего администратора.");

        _context.UserRoles.RemoveRange(roles);
        await _context.SaveChangesAsync();
    }
}