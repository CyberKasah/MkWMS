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
    private readonly ITokenService _tokenService;

    public UsersController(UserService userService, UserRoleService userRoleService, ITokenService tokenService)
    {
        _userService = userService;
        _userRoleService = userRoleService;
        _tokenService = tokenService;
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


    [HttpGet("{id}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetByIdWithRolesAsync(id);
        if (user == null)
            return NotFound($"Пользователь с ID {id} не найден");

        return Ok(user);
    }


    [HttpPost]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> Create(CreateUserWithRolesDto dto)
    {
        if (await _userService.GetByLoginAsync(dto.Login) != null)
            return BadRequest($"Пользователь с логином '{dto.Login}' уже существует");


        if (dto.RoleIds != null)
        {
            foreach (var roleId in dto.RoleIds)
            {
                if (!await _userService.RoleExistsAsync(roleId))
                    return BadRequest($"Роль с ID {roleId} не существует");
            }
        }

        var created = await _userService.CreateWithRolesAsync(dto);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }


    [HttpPut("{id}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> Update(int id, UpdateUserDto dto)
    {
        var user = await _userService.UpdateAsync(id, dto);
        if (user == null)
            return NotFound($"Пользователь с ID {id} не найден");

        return Ok(user);
    }


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


    [HttpPatch("{id}/deactivate")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> Deactivate(int id)
    {
        if (id == GetCurrentUserId())
            return BadRequest("Нельзя деактивировать свою учетную запись");

        var success = await _userService.DeactivateAsync(id);
        if (!success)
            return NotFound($"Пользователь с ID {id} не найден");

        await _tokenService.RevokeAllForUserAsync(id);

        return NoContent();
    }


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





    [HttpPost("{id}/reset-password")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> ResetPassword(int id, AdminResetPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 4)
            return BadRequest("Новый пароль должен содержать минимум 4 символа");


        var adminId = GetCurrentUserId();
        var admin = await _userService.GetByIdAsync(adminId);
        if (admin == null)
            return Unauthorized("Не удалось определить администратора");

        bool adminPasswordValid;
        try
        {
            adminPasswordValid = BCrypt.Net.BCrypt.Verify(dto.AdminPassword, admin.PasswordHash);
        }
        catch (Exception)
        {
            adminPasswordValid = false;
        }

        if (!adminPasswordValid)
            return BadRequest("Неверный пароль администратора");

        var target = await _userService.GetByIdAsync(id);
        if (target == null)
            return NotFound($"Пользователь с ID {id} не найден");

        target.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);



        target.RequiresPasswordChange = false;
        await _userService.UpdateUserAsync(target);
        await _tokenService.RevokeAllForUserAsync(target.Id);

        return Ok(new { Message = $"Пароль пользователя «{target.Login}» успешно изменён" });
    }





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