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
    public decimal PurchasePrice { get; set; }
    public decimal VatRate { get; set; } 
    public decimal TotalValue { get; set; }
    public decimal TotalVat { get; set; }
    public string? LocationName { get; set; }
    public string? RfidTag { get; set; }
    public string? WarehouseName { get; set; }
}