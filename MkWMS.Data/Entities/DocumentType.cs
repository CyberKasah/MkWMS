using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace MkWMS.Data.Entities;

public class DocumentType
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    public ICollection<Document> Documents { get; set; } = new List<Document>();
}