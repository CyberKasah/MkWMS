using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace MkWMS.Data.Entities;

public class User
{
    public int Id { get; set; }
    public string Login { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? FullName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }

    public bool RequiresPasswordChange { get; set; } = false;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<Document> CreatedDocuments { get; set; } = new List<Document>();
    public int? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
}