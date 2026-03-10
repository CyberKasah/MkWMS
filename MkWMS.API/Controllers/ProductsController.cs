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
    public async Task<ActionResult<PagedResult<ProductDto>>> GetAll([FromQuery] PagedRequestDto req)
    {
        if (req.Page < 1) req.Page = 1;
        if (req.PageSize < 1 || req.PageSize > 100) req.PageSize = 20;

        var query = _context.Products.AsNoTracking().AsQueryable();

        // Поиск
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.ToLower().Trim();
            query = query.Where(p =>
                p.Name.ToLower().Contains(s) ||
                (p.Article != null && p.Article.ToLower().Contains(s)) ||
                (p.Barcode != null && p.Barcode.ToLower().Contains(s)));
        }

        // Сортировка
        query = req.SortBy?.ToLower() switch
        {
            "name" => req.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "article" => req.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(p => p.Article) : query.OrderBy(p => p.Article),
            "barcode" => req.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(p => p.Barcode) : query.OrderBy(p => p.Barcode),
            "createddate" => req.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(p => p.CreatedDate) : query.OrderBy(p => p.CreatedDate),
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
                CreatedDate = x.CreatedDate
            })
            .ToListAsync();

        return Ok(new PagedResult<ProductDto>
        {
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)req.PageSize),
            Items = data
        });
    }

    [HttpPost]
    [Authorize(Policy = "AdminPolicy")]
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
            CreatedDate = DateTime.UtcNow
        };

        _context.Products.Add(entity);
        await _context.SaveChangesAsync();

        dto.Id = entity.Id;
        return CreatedAtAction(nameof(GetAll), new { id = dto.Id }, dto);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminPolicy")]
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
    [AllowAnonymous] // можно убрать, если нужен только авторизованный доступ
    public async Task<IActionResult> GetByBarcode(string barcode)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Barcode == barcode);

        if (product == null)
            return NotFound($"Товар со штрихкодом {barcode} не найден");

        return Ok(new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Article = product.Article,
            Barcode = product.Barcode,
            Unit = product.Unit,
            UseSerialNumbers = product.UseSerialNumbers,
            UseBatches = product.UseBatches,
            CreatedDate = product.CreatedDate
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
    <title>Этикетка {product.Name}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin:0; padding:10px; }}
        .label {{ 
            width: 100mm; height: 50mm; border: 1px solid #000; 
            margin: 10px auto; padding: 8px; box-sizing: border-box; 
            text-align: center; page-break-after: always;
        }}
        .name {{ font-size: 18px; font-weight: bold; margin: 4px 0; }}
        .barcode {{ font-size: 24px; letter-spacing: 2px; margin: 8px 0; }}
        img {{ max-width: 80%; height: auto; }}
    </style>
</head>
<body>
";

        for (int i = 0; i < qty; i++)
        {
            html += $@"
    <div class='label'>
        <div class='name'>{product.Name}</div>
        <div>Арт: {product.Article ?? "—"}</div>
        <img src='https://api.qrserver.com/v1/create-qr-code/?size=180x180&data={Uri.EscapeDataString(product.Barcode ?? product.Article ?? product.Name)}' alt='QR'>
        <div class='barcode'>{product.Barcode ?? "—"}</div>
    </div>";
        }

        html += "</body></html>";

        return Content(html, "text/html");
    }
}