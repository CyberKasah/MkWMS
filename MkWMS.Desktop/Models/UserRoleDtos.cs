namespace MkWMS.API.DTOs;

// DTO для создания пользователя с ролями
public class CreateUserWithRolesDto
{
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public List<int> RoleIds { get; set; } = new();
    public int? WarehouseId { get; set; }
}

// DTO для назначения ролей пользователю
public class AssignRolesDto
{
    public int UserId { get; set; }
    public List<int> RoleIds { get; set; } = new();
}

// DTO для информации о связи пользователя и роли
public class UserRoleInfoDto
{
    public int UserId { get; set; }
    public string UserLogin { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
}