using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MkWMS.API.DTOs;
using MkWMS.Data.Context;
using MkWMS.API.Services;

namespace MkWMS.API.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Policy = "AdminPolicy")]
public class RolesController : ControllerBase
{
    private readonly MkWMSDbContext _context;
    private readonly UserService _userService;

    public RolesController(MkWMSDbContext context, UserService userService)
    {
        _context = context;
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<RoleDto>>> GetAll([FromQuery] PagedRequestDto req)
    {
        if (req.Page < 1) req.Page = 1;
        if (req.PageSize < 1 || req.PageSize > 100) req.PageSize = 20;

        var query = _context.Roles.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.ToLower().Trim();
            query = query.Where(r => r.Name.ToLower().Contains(s));
        }

        query = req.SortBy?.ToLower() switch
        {
            "name" => req.SortDirection?.ToLower() == "desc"
                ? query.OrderByDescending(r => r.Name)
                : query.OrderBy(r => r.Name),

            _ => query.OrderBy(r => r.Id)
        };

        var totalCount = await query.CountAsync();

        var data = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name
            })
            .ToListAsync();

        return Ok(new PagedResult<RoleDto>
        {
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)req.PageSize),
            Items = data
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(RoleDto dto)
    {
        var entity = new MkWMS.Data.Entities.Role { Name = dto.Name };
        _context.Roles.Add(entity);
        await _context.SaveChangesAsync();
        dto.Id = entity.Id;
        return Ok(dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, RoleDto dto)
    {
        var entity = await _context.Roles.FindAsync(id);
        if (entity == null) return NotFound();
        entity.Name = dto.Name;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.Roles.FindAsync(id);
        if (entity == null) return NotFound();
        _context.Roles.Remove(entity);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/users")]
    public async Task<IActionResult> GetUsersInRole(int id)
    {
        if (!await _context.Roles.AnyAsync(r => r.Id == id))
            return NotFound($"Роль с ID {id} не найдена");

        var users = await _userService.GetUsersByRoleAsync(id);
        return Ok(users);
    }

    [HttpGet("{id}/not-in")]
    public async Task<IActionResult> GetUsersNotInRole(int id)
    {
        if (!await _context.Roles.AnyAsync(r => r.Id == id))
            return NotFound($"Роль с ID {id} не найдена");

        var users = await _userService.GetUsersNotInRoleAsync(id);
        return Ok(users);
    }
}