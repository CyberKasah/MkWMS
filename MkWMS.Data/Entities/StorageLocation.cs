namespace MkWMS.Data.Entities;

public class StorageLocation
{
    public int Id { get; set; }
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? RfidTag { get; set; }
    public string? CellType { get; set; }
}