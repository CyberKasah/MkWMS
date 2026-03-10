using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MkWMS.API.DTOs;
using MkWMS.Data.Context;
using MkWMS.Data.Entities;

namespace MkWMS.API.Controllers;

[ApiController]
[Route("api/documenttypes")]
[Authorize(Policy = "AdminPolicy")]
public class DocumentTypesController : ControllerBase
{
    private readonly MkWMSDbContext _context;

    public DocumentTypesController(MkWMSDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<DocumentTypeDto>>> GetAll([FromQuery] PagedRequestDto req)
    {
        if (req.Page < 1) req.Page = 1;
        if (req.PageSize < 1 || req.PageSize > 100) req.PageSize = 20;

        var query = _context.DocumentTypes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.ToLower().Trim();
            query = query.Where(x => x.Name.ToLower().Contains(s));
        }

        query = req.SortBy?.ToLower() == "name" && req.SortDirection?.ToLower() == "desc"
            ? query.OrderByDescending(x => x.Name)
            : query.OrderBy(x => x.Name);

        var totalCount = await query.CountAsync();

        var data = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(x => new DocumentTypeDto
            {
                Id = x.Id,
                Name = x.Name
            }).ToListAsync();

        return Ok(new PagedResult<DocumentTypeDto>
        {
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)req.PageSize),
            Items = data
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(DocumentTypeDto dto)
    {
        var entity = new DocumentType { Name = dto.Name };
        _context.DocumentTypes.Add(entity);
        await _context.SaveChangesAsync();

        dto.Id = entity.Id;
        return Ok(dto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.DocumentTypes.FindAsync(id);
        if (entity == null) return NotFound();

        _context.DocumentTypes.Remove(entity);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> Update(int id, DocumentTypeDto dto)
    {
        var entity = await _context.DocumentTypes.FindAsync(id);
        if (entity == null) return NotFound();
        entity.Name = dto.Name;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}