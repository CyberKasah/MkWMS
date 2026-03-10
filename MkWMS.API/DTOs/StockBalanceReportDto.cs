namespace MkWMS.API.DTOs;

public class StockBalanceReportDto
{
    public int ProductId { get; set; }
    public string Product { get; set; } = "";

    public int WarehouseId { get; set; }
    public string Warehouse { get; set; } = "";

    public string? Batch { get; set; }

    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "";
}