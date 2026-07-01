namespace MkWMS.API.DTOs;

public class DocumentDto
{
    public int Id { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime CreatedDate { get; set; }

    public int DocumentTypeId { get; set; }
    public int WarehouseId { get; set; }
    public int? DepartmentId { get; set; }
    public int CreatedByUserId { get; set; }
    public List<DocumentItemDto> Items { get; set; } = new();


    public int? CounterpartyId { get; set; }
    public string? CounterpartyName { get; set; }
    public string? ExternalNumber { get; set; }
    public DateTime? ExternalDate { get; set; }
    public string DocumentTypeName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
}