namespace MkWMS.API.DTOs;

public class CreateUserWithRolesDto
{
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public List<int> RoleIds { get; set; } = new();
    public int? WarehouseId { get; set; }
}

public class AssignRolesDto
{
    public int UserId { get; set; }
    public List<int> RoleIds { get; set; } = new();
}

public class UserRoleInfoDto
{
    public int UserId { get; set; }
    public string UserLogin { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
}