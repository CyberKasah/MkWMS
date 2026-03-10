namespace MkWMS.API.DTOs;

public class DepartmentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
}