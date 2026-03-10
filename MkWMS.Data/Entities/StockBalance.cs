using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MkWMS.Data.Entities;

public class StockBalance
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int WarehouseId { get; set; }
    public int? BatchId { get; set; }
    public decimal Quantity { get; set; }

    // Навигации
    public Product Product { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
    public Batch? Batch { get; set; }
}