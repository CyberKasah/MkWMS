namespace MkWMS.API.Services;

public interface IAuditService
{
    Task LogAsync(int userId, string action);
}