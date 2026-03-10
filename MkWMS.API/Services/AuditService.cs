using MkWMS.Data.Context;
using MkWMS.Data.Entities;

namespace MkWMS.API.Services;

public class AuditService : IAuditService
{
    private readonly MkWMSDbContext _context;

    public AuditService(MkWMSDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(int userId, string action)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            ActionDate = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }
}