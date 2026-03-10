namespace MkWMS.API.DTOs;

public class StockMovementDto
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public int ProductId { get; set; }
    public int WarehouseId { get; set; }
    public int? BatchId { get; set; }
    public int? SerialNumberId { get; set; }
    public decimal QuantityChange { get; set; }
    public DateTime MovementDate { get; set; }
}