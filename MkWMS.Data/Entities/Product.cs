using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MkWMS.Data.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Article { get; set; }
    public string? Barcode { get; set; }
    public string? Unit { get; set; }
    public bool UseSerialNumbers { get; set; }
    public bool UseBatches { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? RfidBaseTag { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal RetailPrice { get; set; }
    public decimal VatRate { get; set; } = 22;
    public bool IsMarked { get; set; } = false;
    public bool IsVet { get; set; } = false;
}
