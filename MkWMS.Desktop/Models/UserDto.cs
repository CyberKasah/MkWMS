namespace MkWMS.API.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string? FullName { get; set; }

    public bool? IsActive { get; set; } 
    public DateTime? CreatedDate { get; set; }   
    public List<RoleDto> Roles { get; set; } = new();
    public int? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
}