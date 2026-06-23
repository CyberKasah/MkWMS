namespace MkWMS.API.DTOs;

public class StorageLocationDto
{
    public int Id { get; set; }
    public int WarehouseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? RfidTag { get; set; }
    public string? CellType { get; set; }
}