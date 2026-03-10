using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MkWMS.Data.Enums;

namespace MkWMS.Data.Entities;

public class Document
{
    public int Id { get; set; }
    public string DocumentNumber { get; set; } = null!;
    public DocumentStatus Status { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedDate { get; set; }

    public int DocumentTypeId { get; set; }
    public DocumentType DocumentType { get; set; } = null!;

    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public ICollection<DocumentItem> Items { get; set; } = new List<DocumentItem>();
}