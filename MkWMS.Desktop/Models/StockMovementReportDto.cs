namespace MkWMS.API.DTOs;

public class StockMovementReportDto
{
    public DateTime Date { get; set; }
    public string Document { get; set; } = "";

    public string Product { get; set; } = "";
    public string Warehouse { get; set; } = "";

    public decimal Quantity { get; set; }
    public string Type { get; set; } = "";
}