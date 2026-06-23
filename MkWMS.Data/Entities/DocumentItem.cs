using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MkWMS.Data.Entities;

public class DocumentItem
{
    public int Id { get; set; }

    public int DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int? BatchId { get; set; }
    public Batch? Batch { get; set; }

    public int? SerialNumberId { get; set; }
    public SerialNumber? SerialNumber { get; set; }

    public decimal Quantity { get; set; }
    public decimal? Price { get; set; }
    public decimal VatSum { get; set; } = 0;
    public decimal Sum => Quantity * (Price ?? 0);
}

