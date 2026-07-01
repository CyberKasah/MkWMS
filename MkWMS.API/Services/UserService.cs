using Microsoft.EntityFrameworkCore;
using MkWMS.Data.Context;
using MkWMS.Data.Entities;
using MkWMS.API.DTOs;

namespace MkWMS.API.Services;

public class UserService
{
    private readonly MkWMSDbContext _context;

    public UserService(MkWMSDbContext context)
    {
        _context = context;
    }



    public async Task<List<UserDto>> GetAllWithRolesAsync()
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Login = u.Login,
                FullName = u.FullName,
                IsActive = u.IsActive,
                CreatedDate = u.CreatedDate,
                Roles = u.UserRoles.Select(ur => new RoleDto
                {
                    Id = ur.Role.Id,
                    Name = ur.Role.Name
                }).ToList()
            })
            .ToListAsync();
    }


    public async Task<UserDto?> GetByIdWithRolesAsync(int id)
    {
        return await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.Warehouse)
            .Where(u => u.Id == id)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Login = u.Login,
                FullName = u.FullName,
                IsActive = u.IsActive,
                CreatedDate = u.CreatedDate,
                WarehouseId = u.WarehouseId,
                WarehouseName = u.Warehouse != null ? u.Warehouse.Name : "Не назначен",
                Roles = u.UserRoles.Select(ur => new RoleDto
                {
                    Id = ur.Role.Id,
                    Name = ur.Role.Name
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }


    public async Task<UserDto?> GetByLoginWithRolesAsync(string login)
    {
        return await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.Warehouse)
            .Where(u => u.Login == login)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Login = u.Login,
                FullName = u.FullName,
                IsActive = u.IsActive,
                CreatedDate = u.CreatedDate,
                WarehouseId = u.WarehouseId,
                WarehouseName = u.Warehouse != null ? u.Warehouse.Name : "Не назначен",
                Roles = u.UserRoles.Select(ur => new RoleDto
                {
                    Id = ur.Role.Id,
                    Name = ur.Role.Name
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }


    public async Task<UserDto> CreateWithRolesAsync(CreateUserWithRolesDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {

            if (dto.WarehouseId.HasValue)
            {
                if (!await _context.Warehouses.AnyAsync(w => w.Id == dto.WarehouseId.Value && w.IsActive))
                    throw new InvalidOperationException($"Склад с ID {dto.WarehouseId} не существует или неактивен");
            }
            var user = new User
            {
                Login = dto.Login,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                FullName = dto.FullName,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,




                RequiresPasswordChange = false,
                WarehouseId = dto.WarehouseId
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            if (dto.RoleIds != null && dto.RoleIds.Any())
            {
                foreach (var roleId in dto.RoleIds)
                {
                    if (!await _context.Roles.AnyAsync(r => r.Id == roleId))
                        throw new InvalidOperationException($"Роль с ID {roleId} не существует");
                    _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
                }
                await _context.SaveChangesAsync();
            }
            await transaction.CommitAsync();
            return await GetByIdWithRolesAsync(user.Id) ?? throw new Exception("Ошибка создания");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }


    public async Task<UserDto?> UpdateAsync(int id, UpdateUserDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null;
        if (dto.FullName != null) user.FullName = dto.FullName;
        if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;
        if (!string.IsNullOrEmpty(dto.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        if (dto.WarehouseId.HasValue)
        {
            if (dto.WarehouseId.Value != user.WarehouseId)
            {
                if (!await _context.Warehouses.AnyAsync(w => w.Id == dto.WarehouseId.Value && w.IsActive))
                    throw new InvalidOperationException($"Склад с ID {dto.WarehouseId} не существует или неактивен");
                user.WarehouseId = dto.WarehouseId.Value;
            }
        }
        await _context.SaveChangesAsync();
        return await GetByIdWithRolesAsync(id);
    }


    public async Task<bool> DeleteAsync(int id)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return false;
        _context.UserRoles.RemoveRange(user.UserRoles);
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }



    public async Task<List<UserDto>> GetUsersByRoleAsync(int roleId)
    {
        return await _context.UserRoles
            .Where(ur => ur.RoleId == roleId)
            .Include(ur => ur.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .Select(ur => new UserDto
            {
                Id = ur.User.Id,
                Login = ur.User.Login,
                FullName = ur.User.FullName,
                IsActive = ur.User.IsActive,
                CreatedDate = ur.User.CreatedDate,
                Roles = ur.User.UserRoles.Select(r => new RoleDto
                {
                    Id = r.Role.Id,
                    Name = r.Role.Name
                }).ToList()
            })
            .ToListAsync();
    }


    public async Task<List<UserDto>> GetUsersNotInRoleAsync(int roleId)
    {
        var usersInRole = await _context.UserRoles
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => ur.UserId)
            .ToListAsync();
        return await _context.Users
            .Where(u => !usersInRole.Contains(u.Id))
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Login = u.Login,
                FullName = u.FullName,
                IsActive = u.IsActive,
                CreatedDate = u.CreatedDate,
                Roles = u.UserRoles.Select(ur => new RoleDto
                {
                    Id = ur.Role.Id,
                    Name = ur.Role.Name
                }).ToList()
            })
            .ToListAsync();
    }


    public async Task<PagedResult<UserDto>> GetPagedAsync(
     string? search,
     bool? isActive,
     int page,
     int pageSize,
     string? sortBy,
     string? sortDirection)
    {
        var query = _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(u =>
                (u.Login != null && u.Login.ToLower().Contains(search)) ||
                (u.FullName != null && u.FullName.ToLower().Contains(search))
            );
        }

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive);

        sortBy = sortBy?.ToLower();
        sortDirection = sortDirection?.ToLower() == "desc" ? "desc" : "asc";
        query = sortBy switch
        {
            "login" => sortDirection == "desc"
                ? query.OrderByDescending(u => u.Login)
                : query.OrderBy(u => u.Login),
            "fullname" => sortDirection == "desc"
                ? query.OrderByDescending(u => u.FullName)
                : query.OrderBy(u => u.FullName),
            "createddate" => sortDirection == "desc"
                ? query.OrderByDescending(u => u.CreatedDate)
                : query.OrderBy(u => u.CreatedDate),
            _ => query.OrderBy(u => u.Id)
        };
        var totalCount = await query.CountAsync();
        var users = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Login = u.Login,
                FullName = u.FullName,
                IsActive = u.IsActive,
                CreatedDate = u.CreatedDate,

                WarehouseId = u.WarehouseId,
                WarehouseName = u.Warehouse != null ? u.Warehouse.Name : "Не назначен",

                Roles = u.UserRoles.Select(ur => new RoleDto
                {
                    Id = ur.Role.Id,
                    Name = ur.Role.Name
                }).ToList()
            })
            .ToListAsync();
        return new PagedResult<UserDto>
        {
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = users
        };
    }


    public async Task<List<User>> GetAllAsync()
    {
        return await _context.Users.AsNoTracking().ToListAsync();
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<User?> GetByLoginAsync(string login)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Login == login);
    }

    public async Task<User> CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> UpdateUserAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;
        user.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<string>> GetRolesAsync(int userId)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .ToListAsync();
    }

    public async Task AssignRoleAsync(int userId, int roleId)
    {
        var exists = await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
        if (!exists)
        {
            _context.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveRoleAsync(int userId, int roleId)
    {
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
        if (userRole != null)
        {
            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> RoleExistsAsync(int roleId)
    {
        return await _context.Roles.AnyAsync(r => r.Id == roleId);
    }


    public async Task<Role?> GetRoleByNameAsync(string name)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == name);
    }
}