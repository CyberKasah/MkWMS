namespace MkWMS.API.DTOs;

public class AuditLogDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? UserLogin { get; set; }

    public string? UserFullName { get; set; }

    public string? UserRole { get; set; }

    public string Action { get; set; } = string.Empty;
    public DateTime ActionDate { get; set; }
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public string? ChangesJson { get; set; }
}