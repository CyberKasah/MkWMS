namespace MkWMS.API.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Article { get; set; }
    public string? Barcode { get; set; }
    public string? Unit { get; set; }
    public bool UseSerialNumbers { get; set; }
    public bool UseBatches { get; set; }
    public DateTime CreatedDate { get; set; }

    // Новые поля
    public decimal PurchasePrice { get; set; }
    public decimal RetailPrice { get; set; }
    public decimal VatRate { get; set; } 
    public bool IsMarked { get; set; } 
    public bool IsVet { get; set; }
    public string? RfidBaseTag { get; set; }
}