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
    public string FromLocation { get; set; } = string.Empty;
    public string ToLocation { get; set; } = string.Empty;
    public string RfidTag { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal VatRate { get; set; }
    public decimal VatSum { get; set; } 
    public decimal TotalValue { get; set; }

}