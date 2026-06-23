using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MkWMS.Data.Entities;

public class StockMovement
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public int ProductId { get; set; }
    public int WarehouseId { get; set; }
    public int? BatchId { get; set; }
    public int? SerialNumberId { get; set; }
    public decimal QuantityChange { get; set; }
    public DateTime MovementDate { get; set; }
    public decimal? Price { get; set; }
    public int? StorageLocationId { get; set; }


    // Навигации
    public Product Product { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
    public Batch? Batch { get; set; }
    public SerialNumber? SerialNumber { get; set; }
    public Document Document { get; set; } = null!;
    public StorageLocation? StorageLocation { get; set; }
}