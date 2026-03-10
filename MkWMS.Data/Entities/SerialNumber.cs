using MkWMS.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MkWMS.Data.Entities;

public class SerialNumber
{
    public int Id { get; set; }
    public string Number { get; set; } = null!;
    public int Status { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
