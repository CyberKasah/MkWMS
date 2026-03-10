using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MkWMS.API.DTOs;
using MkWMS.Data.Context;
using MkWMS.Data.Entities;

namespace MkWMS.API.Controllers;

[ApiController]
[Route("api/batches")]
[Authorize]
public class BatchesController : ControllerBase
{
    private readonly MkWMSDbContext _context;

    public BatchesController(MkWMSDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<BatchDto>>> GetAll([FromQuery] PagedRequestDto req)
    {
        if (req.Page < 1) req.Page = 1;
        if (req.PageSize < 1 || req.PageSize > 100) req.PageSize = 20;

        var query = _context.Batches.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.ToLower().Trim();
            query = query.Where(x => x.BatchNumber.ToLower().Contains(s));
        }

        query = req.SortBy?.ToLower() switch
        {
            "batchnumber" => req.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(x => x.BatchNumber) : query.OrderBy(x => x.BatchNumber),
            "productiondate" => req.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(x => x.ProductionDate) : query.OrderBy(x => x.ProductionDate),
            "expirationdate" => req.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(x => x.ExpirationDate) : query.OrderBy(x => x.ExpirationDate),
            _ => query.OrderBy(x => x.Id)
        };

        var totalCount = await query.CountAsync();

        var data = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(x => new BatchDto
            {
                Id = x.Id,
                BatchNumber = x.BatchNumber,
                ProductionDate = x.ProductionDate,
                ExpirationDate = x.ExpirationDate,
                ProductId = x.ProductId
            }).ToListAsync();

        return Ok(new PagedResult<BatchDto>
        {
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)req.PageSize),
            Items = data
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var entity = await _context.Batches.FindAsync(id);
        if (entity == null) return NotFound();
        return Ok(new BatchDto
        {
            Id = entity.Id,
            BatchNumber = entity.BatchNumber,
            ProductionDate = entity.ProductionDate,
            ExpirationDate = entity.ExpirationDate,
            ProductId = entity.ProductId
        });
    }

    [HttpPost]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> Create(BatchDto dto)
    {
        var entity = new Batch
        {
            BatchNumber = dto.BatchNumber,
            ProductionDate = dto.ProductionDate,
            ExpirationDate = dto.ExpirationDate,
            ProductId = dto.ProductId
        };

        _context.Batches.Add(entity);
        await _context.SaveChangesAsync();

        dto.Id = entity.Id;
        return Ok(dto);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> Update(int id, BatchDto dto)
    {
        var entity = await _context.Batches.FindAsync(id);
        if (entity == null) return NotFound();

        entity.BatchNumber = dto.BatchNumber;
        entity.ProductionDate = dto.ProductionDate;
        entity.ExpirationDate = dto.ExpirationDate;
        entity.ProductId = dto.ProductId;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.Batches.FindAsync(id);
        if (entity == null) return NotFound();

        _context.Batches.Remove(entity);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}