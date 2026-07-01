using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MkWMS.API.DTOs;
using MkWMS.Data.Context;
using MkWMS.Data.Entities;

namespace MkWMS.API.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly MkWMSDbContext _context;

    public ProductsController(MkWMSDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetAll([FromQuery] PagedRequestDto req)
    {
        if (req.Page < 1) req.Page = 1;
        if (req.PageSize < 1 || req.PageSize > 100) req.PageSize = 20;

        var query = _context.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(req.Search) && req.Search.Trim().Length > 0)
        {
            var s = req.Search.ToLower().Trim();
            query = query.Where(p =>
                p.Name.ToLower().Contains(s) ||
                (p.Article != null && p.Article.ToLower().Contains(s)) ||
                (p.Barcode != null && p.Barcode.ToLower().Contains(s)));
        }

        query = req.SortBy?.ToLower() switch
        {
            "name" => req.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "article" => req.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(p => p.Article) : query.OrderBy(p => p.Article),
            _ => query.OrderBy(p => p.Id)
        };

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(x => new ProductDto
            {
                Id = x.Id,
                Name = x.Name,
                Article = x.Article,
                Barcode = x.Barcode,
                Unit = x.Unit,
                UseSerialNumbers = x.UseSerialNumbers,
                UseBatches = x.UseBatches,
                CreatedDate = x.CreatedDate,
                PurchasePrice = x.PurchasePrice,
                RetailPrice = x.RetailPrice,
                VatRate = x.VatRate,
                IsMarked = x.IsMarked,
                IsVet = x.IsVet,
                RfidBaseTag = x.RfidBaseTag
            })
            .ToListAsync();

        return Ok(new PagedResult<ProductDto> { Items = data, TotalCount = totalCount, Page = req.Page, PageSize = req.PageSize });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var x = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (x == null) return NotFound();

        return Ok(new ProductDto
        {
            Id = x.Id,
            Name = x.Name,
            Article = x.Article,
            Barcode = x.Barcode,
            Unit = x.Unit,
            UseSerialNumbers = x.UseSerialNumbers,
            UseBatches = x.UseBatches,
            CreatedDate = x.CreatedDate,
            PurchasePrice = x.PurchasePrice,
            RetailPrice = x.RetailPrice,
            VatRate = x.VatRate,
            IsMarked = x.IsMarked,
            IsVet = x.IsVet,
            RfidBaseTag = x.RfidBaseTag
        });
    }




    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(ProductDto dto)
    {
        var entity = new Product
        {
            Name = dto.Name,
            Article = dto.Article,
            Barcode = dto.Barcode,
            Unit = dto.Unit,
            UseSerialNumbers = dto.UseSerialNumbers,
            UseBatches = dto.UseBatches,
            CreatedDate = DateTime.UtcNow,

            PurchasePrice = dto.PurchasePrice,
            RetailPrice = dto.RetailPrice,
            VatRate = dto.VatRate,
            IsMarked = dto.IsMarked,
            IsVet = dto.IsVet,
            RfidBaseTag = dto.RfidBaseTag
        };

        _context.Products.Add(entity);
        await _context.SaveChangesAsync();

        dto.Id = entity.Id;
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, dto);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, ProductDto dto)
    {
        var entity = await _context.Products.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Name = dto.Name;
        entity.Article = dto.Article;
        entity.Barcode = dto.Barcode;
        entity.Unit = dto.Unit;
        entity.UseSerialNumbers = dto.UseSerialNumbers;
        entity.UseBatches = dto.UseBatches;

        entity.PurchasePrice = dto.PurchasePrice;
        entity.RetailPrice = dto.RetailPrice;
        entity.VatRate = dto.VatRate;
        entity.IsMarked = dto.IsMarked;
        entity.IsVet = dto.IsVet;
        entity.RfidBaseTag = dto.RfidBaseTag;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.Products.FindAsync(id);
        if (entity == null) return NotFound();

        _context.Products.Remove(entity);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("by-barcode/{barcode}")]
    public async Task<IActionResult> GetByBarcode(string barcode)
    {
        var x = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Barcode == barcode);

        if (x == null)
            return NotFound($"Товар со штрихкодом {barcode} не найден");

        return Ok(new ProductDto
        {
            Id = x.Id,
            Name = x.Name,
            Article = x.Article,
            Barcode = x.Barcode,
            Unit = x.Unit,
            UseSerialNumbers = x.UseSerialNumbers,
            UseBatches = x.UseBatches,
            CreatedDate = x.CreatedDate,
            PurchasePrice = x.PurchasePrice,
            RetailPrice = x.RetailPrice,
            VatRate = x.VatRate,
            IsMarked = x.IsMarked,
            IsVet = x.IsVet,
            RfidBaseTag = x.RfidBaseTag
        });
    }

    [HttpGet("label/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLabel(int id, [FromQuery] int qty = 1)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Этикетки {product.Name}</title>
    <style>
        body {{
            margin: 0;
            padding: 10mm;
            font-family: Arial, sans-serif;
        }}
        .labels-grid {{
            display: grid;
            grid-template-columns: repeat(3, 100mm);  
            grid-gap: 10mm 8mm;
            justify-content: center;
        }}
        .label {{
            width: 100mm;
            height: 100mm;  
            border: 1px solid #000;
            padding: 6mm;
            box-sizing: border-box;
            text-align: center;
            page-break-inside: avoid;
            overflow: hidden;
            background: white;
        }}
        .name {{
            font-size: 18px;
            font-weight: bold;
            margin: 2mm 0 1mm;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
            max-width: 88mm;
        }}
        .article {{
            font-size: 14px;
            margin: 1mm 0 3mm;
        }}
        .qr {{
            width: 70mm;
            height: 70mm;
            margin: 4mm auto 3mm;
        }}
        .barcode {{
            font-size: 16px;
            letter-spacing: 1px;
            font-weight: bold;
            margin-top: 2mm;
            white-space: nowrap;
        }}
        @media print {{
            .labels-grid {{
                grid-template-columns: repeat(3, 100mm);
                grid-gap: 10mm 8mm;
            }}
            .label {{
                break-inside: avoid;
                page-break-inside: avoid;
            }}
            body {{
                padding: 0;
                margin: 0;
            }}
        }}
    </style>
</head>
<body>
    <div class='labels-grid'>
";

        for (int i = 0; i < qty; i++)
        {
            html += $@"
        <div class='label'>
            <div class='name'>{product.Name}</div>
            <div class='article'>Арт: {product.Article ?? "—"}</div>
            <img class='qr' src='https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={Uri.EscapeDataString(product.Barcode ?? product.Article ?? product.Name)}' alt='QR'>
            <div class='barcode'>{product.Barcode ?? "—"}</div>
        </div>";
        }

        html += @"
    </div>
</body>
</html>";

        return Content(html, "text/html");
    }
}