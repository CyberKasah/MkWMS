namespace MkWMS.API.DTOs;

public class UpdateUserDto
{
    public string? FullName { get; set; }
    public bool? IsActive { get; set; }
    public string? Password { get; set; }

    public int? WarehouseId { get; set; }
}