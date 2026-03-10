using System.Collections.ObjectModel;

namespace MkWMS.API.DTOs;

public class CreateDocumentDto
{
    public string Number { get; set; } = null!;
    public int DocumentTypeId { get; set; }
    public int WarehouseId { get; set; }
    public ObservableCollection<CreateDocumentItemDto> Items { get; set; } = new();  
}

public class CreateDocumentItemDto
{
    public int ProductId { get; set; }
    public int? BatchId { get; set; }
    public int? SerialNumberId { get; set; }
    public decimal Quantity { get; set; }
}