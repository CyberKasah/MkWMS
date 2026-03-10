namespace MkWMS.API.DTOs;

public class DocumentItemDto
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public int ProductId { get; set; }
    public int? BatchId { get; set; }
    public int? SerialNumberId { get; set; }
    public decimal Quantity { get; set; }
    public decimal? Price { get; set; }
}