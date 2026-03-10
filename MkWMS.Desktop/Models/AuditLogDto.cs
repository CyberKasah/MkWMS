namespace MkWMS.API.DTOs;

public class AuditLogDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public DateTime ActionDate { get; set; }
}