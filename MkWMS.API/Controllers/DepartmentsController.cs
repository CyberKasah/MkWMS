using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MkWMS.API.DTOs;
using MkWMS.Data.Context;
using MkWMS.API.Services;

namespace MkWMS.API.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly MkWMSDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DepartmentsController(MkWMSDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<DepartmentDto>>> GetAll([FromQuery] PagedRequestDto req)
    {
        if (req.Page < 1) req.Page = 1;
        if (req.PageSize < 1 || req.PageSize > 100) req.PageSize = 20;

        var query = _context.Departments.AsNoTracking().AsQueryable();


        if (!_currentUser.CanSeeAllWarehouses && _currentUser.WarehouseId.HasValue)
            query = query.Where(d => d.WarehouseId == _currentUser.WarehouseId.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.ToLower().Trim();
            query = query.Where(d => d.Name.ToLower().Contains(s));
        }

        query = req.SortBy?.ToLower() switch
        {
            "name" => req.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(d => d.Name) : query.OrderBy(d => d.Name),
            _ => query.OrderBy(d => d.Id)
        };

        var totalCount = await query.CountAsync();

        var data = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(x => new DepartmentDto
            {
                Id = x.Id,
                Name = x.Name,
                WarehouseId = x.WarehouseId
            })
            .ToListAsync();

        return Ok(new PagedResult<DepartmentDto>
        {
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)req.PageSize),
            Items = data
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DepartmentDto>> GetById(int id)
    {
        var department = await _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department == null) return NotFound();


        if (!_currentUser.CanSeeAllWarehouses && _currentUser.WarehouseId != department.WarehouseId)
            return Forbid();

        return Ok(new DepartmentDto
        {
            Id = department.Id,
            Name = department.Name,
            WarehouseId = department.WarehouseId
        });
    }

    [HttpPost]
    public async Task<ActionResult<DepartmentDto>> Create([FromBody] DepartmentDto dto)
    {
        var department = new MkWMS.Data.Entities.Department
        {
            Name = dto.Name,
            WarehouseId = dto.WarehouseId
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        dto.Id = department.Id;
        return CreatedAtAction(nameof(GetById), new { id = department.Id }, dto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, [FromBody] DepartmentDto dto)
    {
        if (id != dto.Id) return BadRequest("ID mismatch");

        var department = await _context.Departments.FindAsync(id);
        if (department == null) return NotFound();


        if (!_currentUser.CanSeeAllWarehouses && _currentUser.WarehouseId != department.WarehouseId)
            return Forbid();

        department.Name = dto.Name;
        department.WarehouseId = dto.WarehouseId;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department == null) return NotFound();


        if (!_currentUser.CanSeeAllWarehouses && _currentUser.WarehouseId != department.WarehouseId)
            return Forbid();

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
