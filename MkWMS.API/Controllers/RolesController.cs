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
    public async Task<IActionResult> GetAll() => Ok(await _context.Roles
        .Select(r => new RoleDto { Id = r.Id, Name = r.Name })
        .ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role == null) return NotFound();
        return Ok(new RoleDto { Id = role.Id, Name = role.Name });
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