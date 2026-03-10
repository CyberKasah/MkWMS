namespace MkWMS.API.DTOs;

public class BatchDto
{
    public int Id { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime? ProductionDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public int ProductId { get; set; }
}