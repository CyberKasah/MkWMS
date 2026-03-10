namespace MkWMS.API.Services;

public interface IUserRoleService
{
    Task<bool> AssignRoleAsync(int userId, int roleId);
    Task<bool> RemoveRoleAsync(int userId, int roleId);
    Task<List<string>> GetUserRolesAsync(int userId);
}