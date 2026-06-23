namespace MkWMS.API.DTOs;

public class StockBalanceDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int WarehouseId { get; set; }
    public string? LocationName { get; set; }
    public string? RfidTag { get; set; }
    public int? BatchId { get; set; }
    public decimal Quantity { get; set; }

}