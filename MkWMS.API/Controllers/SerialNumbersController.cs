using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MkWMS.API.DTOs;
using MkWMS.Data.Context;

namespace MkWMS.API.Controllers;

[ApiController]
[Route("api/serialnumbers")]   // ← исправлено
[Authorize(Policy = "AdminPolicy")]
public class SerialNumbersController : ControllerBase
{
    private readonly MkWMSDbContext _context;

    public SerialNumbersController(MkWMSDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<SerialNumberDto>>> GetAll([FromQuery] PagedRequestDto req)
    {
        if (req.Page < 1) req.Page = 1;
        if (req.PageSize < 1 || req.PageSize > 100) req.PageSize = 20;

        var query = _context.SerialNumbers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.ToLower().Trim();
            query = query.Where(x => x.Number.ToLower().Contains(s));
        }

        query = req.SortBy?.ToLower() switch
        {
            "number" => req.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(x => x.Number) : query.OrderBy(x => x.Number),
            _ => query.OrderBy(x => x.Id)
        };

        var totalCount = await query.CountAsync();

        var data = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(x => new SerialNumberDto
            {
                Id = x.Id,
                Number = x.Number,
                Status = x.Status,
                ProductId = x.ProductId
            }).ToListAsync();

        return Ok(new PagedResult<SerialNumberDto>
        {
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)req.PageSize),
            Items = data
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(SerialNumberDto dto)
    {
        var entity = new MkWMS.Data.Entities.SerialNumber
        {
            Number = dto.Number,
            Status = dto.Status,
            ProductId = dto.ProductId
        };

        _context.SerialNumbers.Add(entity);
        await _context.SaveChangesAsync();

        dto.Id = entity.Id;
        return Ok(dto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.SerialNumbers.FindAsync(id);
        if (entity == null) return NotFound();

        _context.SerialNumbers.Remove(entity);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}