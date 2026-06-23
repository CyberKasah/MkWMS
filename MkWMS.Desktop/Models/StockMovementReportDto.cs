namespace MkWMS.API.DTOs;

public class StockMovementReportDto
{
    public DateTime Date { get; set; }
    public string Document { get; set; } = "";

    public string Product { get; set; } = "";
    public string Warehouse { get; set; } = "";

    public decimal Quantity { get; set; }
    public string Type { get; set; } = "";
    public string LocationName { get; set; } = string.Empty;
    public string RfidTag { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal VatRate { get; set; }
    public decimal VatSum { get; set; }
    public decimal TotalValue { get; set; }
    public string? FromLocation { get; set; }
    public string? ToLocation { get; set; }
    public string? DocumentNumber { get; set; }
}