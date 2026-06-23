using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MkWMS.Data.Entities;

public class Batch
{
    public int Id { get; set; }
    public string BatchNumber { get; set; } = null!;
    public DateTime? ProductionDate { get; set; }
    public DateTime? ExpirationDate { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string? VsdUuid { get; set; }
}
