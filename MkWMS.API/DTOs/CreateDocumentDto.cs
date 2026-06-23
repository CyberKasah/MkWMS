namespace MkWMS.API.DTOs;

public class CreateDocumentDto
{
    public string? Number { get; set; }
    public int DocumentTypeId { get; set; }
    public int WarehouseId { get; set; }
    public int? BaseDocumentId { get; set; } // Для контроля лимитов
    public int? CounterpartyId { get; set; }
    public string? ExternalNumber { get; set; }
    public DateTime? ExternalDate { get; set; }
    public string? Comment { get; set; }
    public List<CreateDocumentItemDto> Items { get; set; } = new();
}

public class CreateDocumentItemDto
{
    public int ProductId { get; set; }
    public int? BatchId { get; set; }
    public int? SerialNumberId { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal VatSum { get; set; }
    public decimal TotalSum => (Quantity * Price) + VatSum;
}