using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MkWMS.API.DTOs;
using MkWMS.API.Services;
using MkWMS.Data.Entities;
using System.Security.Claims;

namespace MkWMS.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly UserRoleService _userRoleService;

    public UsersController(UserService userService, UserRoleService userRoleService)
    {
        _userService = userService;
        _userRoleService = userRoleService;
    }

    private int GetCurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    [HttpGet]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> GetAll([FromQuery] PagedRequestDto req)
    {
        if (req.Page < 1) req.Page = 1;
        if (req.PageSize < 1 || req.PageSize > 100) req.PageSize = 10;

        bool? isActive = null;
        if (req.Search?.Contains("active:true") == true) isActive = true;
        if (req.Search?.Contains("active:false") == true) isActive = false;

        var result = await _userService.GetPagedAsync(
            req.Search,
            isActive,
            req.Page,
            req.PageSize,
            req.SortBy,
            req.SortDirection);

        return Ok(result);
    }

    // GET: api/users/{id}
    [HttpGet("{id}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetByIdWithRolesAsync(id);
        if (user == null)
            return NotFound($"Пользователь с ID {id} не найден");

        return Ok(user);
    }

    // POST: api/users
    [HttpPost]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> Create(CreateUserWithRolesDto dto)
    {
        if (await _userService.GetByLoginAsync(dto.Login) != null)
            return BadRequest($"Пользователь с логином '{dto.Login}' уже существует");

        // Проверка существования всех ролей
        foreach (var roleId in dto.RoleIds)
        {
            if (!await _userService.RoleExistsAsync(roleId))
                return BadRequest($"Роль с ID {roleId} не существует");
        }

        var created = await _userService.CreateWithRolesAsync(dto);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT: api/users/{id}
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> Update(int id, UpdateUserDto dto)
    {
        var user = await _userService.UpdateAsync(id, dto);
        if (user == null)
            return NotFound($"Пользователь с ID {id} не найден");

        return Ok(user);
    }

    // DELETE: api/users/{id}
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> Delete(int id)
    {
        if (id == GetCurrentUserId())
            return BadRequest("Нельзя удалить свою учетную запись");

        var success = await _userService.DeleteAsync(id);
        if (!success)
            return NotFound($"Пользователь с ID {id} не найден");

        return NoContent();
    }

    // PATCH: api/users/{id}/deactivate
    [HttpPatch("{id}/deactivate")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> Deactivate(int id)
    {
        if (id == GetCurrentUserId())
            return BadRequest("Нельзя деактивировать свою учетную запись");

        var success = await _userService.DeactivateAsync(id);
        if (!success)
            return NotFound($"Пользователь с ID {id} не найден");

        return NoContent();
    }

    // PATCH: api/users/{id}/activate
    [HttpPatch("{id}/activate")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> Activate(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
            return NotFound($"Пользователь с ID {id} не найден");

        user.IsActive = true;
        await _userService.UpdateUserAsync(user);

        return NoContent();
    }

    // ──────────────────────────────────────────────
    // Управление ролями
    // ──────────────────────────────────────────────

    [HttpGet("{id}/roles")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> GetUserRoles(int id)
    {
        var roles = await _userRoleService.GetUserRolesAsync(id);
        return Ok(roles);
    }

    [HttpPost("{id}/roles")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> AssignRoles(int id, [FromBody] AssignRolesDto dto)
    {
        if (id != dto.UserId)
            return BadRequest("ID в URL и теле не совпадают");

        var success = await _userRoleService.AssignRolesAsync(id, dto.RoleIds);
        if (!success)
            return BadRequest("Ошибка назначения ролей");

        return NoContent();
    }

    [HttpDelete("{id}/roles/{roleId}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> RemoveRole(int id, int roleId)
    {
        try
        {
            var success = await _userRoleService.RemoveRoleAsync(id, roleId);
            if (!success)
                return NotFound("Роль не найдена.");

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}/roles")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> RemoveAllRoles(int id)
    {
        if (id == GetCurrentUserId())
            return BadRequest("Нельзя удалить все роли у себя");

        await _userRoleService.RemoveAllRolesAsync(id);
        return NoContent();
    }

    [HttpGet("role/{roleId}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> GetUsersByRole(int roleId)
    {
        var users = await _userService.GetUsersByRoleAsync(roleId);
        return Ok(users);
    }

    [HttpGet("role/{roleId}/not-in")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> GetUsersNotInRole(int roleId)
    {
        var users = await _userService.GetUsersNotInRoleAsync(roleId);
        return Ok(users);
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        var user = await _userService.GetByIdWithRolesAsync(userId);

        if (user == null)
            return NotFound("Текущий пользователь не найден");

        return Ok(user);
    }
}