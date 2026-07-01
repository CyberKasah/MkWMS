using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MkWMS.API.DTOs;
using MkWMS.Data.Context;
using MkWMS.Data.Entities;
using MkWMS.API.Services;

namespace MkWMS.API.Controllers;

[ApiController]
[Route("api/warehouses")]
[Authorize]
public class WarehousesController : ControllerBase
{
    private readonly MkWMSDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public WarehousesController(MkWMSDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<WarehouseDto>>> GetAll([FromQuery] PagedRequestDto req)
    {
        if (req.Page < 1) req.Page = 1;
        if (req.PageSize < 1 || req.PageSize > 100) req.PageSize = 20;

        var query = _context.Warehouses.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.ToLower().Trim();
            query = query.Where(w => w.Name.ToLower().Contains(s) ||
                                    (w.Address != null && w.Address.ToLower().Contains(s)));
        }

        query = req.SortBy?.ToLower() switch
        {
            "name" => req.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(w => w.Name) : query.OrderBy(w => w.Name),
            "address" => req.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(w => w.Address) : query.OrderBy(w => w.Address),
            _ => query.OrderBy(w => w.Id)
        };

        var totalCount = await query.CountAsync();

        var data = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(x => new WarehouseDto
            {
                Id = x.Id,
                Name = x.Name,
                Address = x.Address,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Ok(new PagedResult<WarehouseDto>
        {
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)req.PageSize),
            Items = data
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var entity = await _context.Warehouses.FindAsync(id);
        if (entity == null) return NotFound();
        return Ok(new WarehouseDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Address = entity.Address,
            IsActive = entity.IsActive
        });
    }




    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(WarehouseDto dto)
    {
        var entity = new Warehouse
        {
            Name = dto.Name,
            Address = dto.Address,
            IsActive = dto.IsActive
        };

        _context.Warehouses.Add(entity);
        await _context.SaveChangesAsync();

        dto.Id = entity.Id;
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }


    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, WarehouseDto dto)
    {
        var entity = await _context.Warehouses.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Name = dto.Name;
        entity.Address = dto.Address;
        entity.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();
        return NoContent();
    }


    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.Warehouses
            .Include(w => w.Departments)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (entity == null) return NotFound();


        if (entity.Departments.Any())
            return BadRequest("Нельзя удалить склад, у которого есть подразделения");

        _context.Warehouses.Remove(entity);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}