using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MkWMS.Data.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = null!;
    public DateTime ActionDate { get; set; }
    public User User { get; set; } = null!;

    // НОВЫЕ ПОЛЯ
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public string? ChangesJson { get; set; }
}
