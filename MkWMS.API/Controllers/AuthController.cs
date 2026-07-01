using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MkWMS.API.DTOs;
using MkWMS.API.Services;
using MkWMS.Data.Entities;
using System.Security.Claims;

namespace MkWMS.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;
    private readonly ITokenService _tokenService;

    public AuthController(UserService userService, ITokenService tokenService)
    {
        _userService = userService;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        try
        {
            if (await _userService.GetByLoginAsync(dto.Login) != null)
                return BadRequest("Пользователь с таким логином уже существует");

            var user = new User
            {
                Login = dto.Login,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                FullName = dto.FullName,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                RequiresPasswordChange = false,
            };

            var created = await _userService.CreateAsync(user);

            if (dto.RoleIds != null && dto.RoleIds.Any())
            {
                foreach (var roleId in dto.RoleIds)
                {
                    var roleExists = await _userService.RoleExistsAsync(roleId);
                    if (!roleExists)
                        return BadRequest($"Роль с Id {roleId} не существует");

                    await _userService.AssignRoleAsync(created.Id, roleId);
                }
            }
            else
            {
                await _userService.AssignRoleAsync(created.Id, 3);
            }

            return Ok(new { Id = created.Id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при регистрации: {ex.Message}");
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _userService.GetByLoginAsync(dto.Login);
        if (user == null)
            return Unauthorized(new { Message = "Неверные учетные данные" });

        if (!user.IsActive)
            return Unauthorized(new { Message = "Пользователь деактивирован" });

        bool passwordValid = false;
        bool requiresPasswordChange = user.RequiresPasswordChange;

        try
        {
            passwordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            requiresPasswordChange = true;
            user.RequiresPasswordChange = true;
            await _userService.UpdateUserAsync(user);
        }
        catch (Exception)
        {
            passwordValid = false;
        }

        if (!passwordValid)
        {
            return Unauthorized(new
            {
                Message = "Неверные учетные данные",
                RequiresPasswordChange = requiresPasswordChange
            });
        }

        var roleNames = await _userService.GetRolesAsync(user.Id);

        var roleDtos = new List<RoleDto>();
        foreach (var roleName in roleNames)
        {
            var role = await _userService.GetRoleByNameAsync(roleName);
            roleDtos.Add(new RoleDto
            {
                Id = role?.Id ?? 0,
                Name = roleName
            });
        }

        var (accessToken, refreshToken) = await _tokenService.IssueTokenPairAsync(user);

        var userResponse = new
        {
            Id = user.Id,
            Login = user.Login,
            FullName = user.FullName,
            Roles = roleDtos
        };

        if (requiresPasswordChange)
        {
            return Ok(new
            {
                Token = accessToken,
                RefreshToken = refreshToken,
                RequiresPasswordChange = true,
                Message = "Требуется смена пароля",
                User = userResponse
            });
        }

        return Ok(new
        {
            Token = accessToken,
            RefreshToken = refreshToken,
            RequiresPasswordChange = false,
            User = userResponse
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(RefreshRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            return Unauthorized(new { Message = "Сессия истекла, требуется повторный вход" });

        var result = await _tokenService.RotateAsync(dto.RefreshToken);
        if (!result.Success)
            return Unauthorized(new { Message = result.Error });

        return Ok(new { Token = result.AccessToken, RefreshToken = result.RefreshToken });
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(RefreshRequestDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.RefreshToken))
            await _tokenService.RevokeAsync(dto.RefreshToken);

        return Ok();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            return Unauthorized("Ошибка идентификации пользователя");

        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
            return NotFound("Пользователь не найден");

        if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
            return BadRequest("Неверный старый пароль");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.RequiresPasswordChange = false;

        await _userService.UpdateUserAsync(user);

        return Ok("Пароль успешно изменён");
    }
}

public class RegisterDto
{
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public List<int> RoleIds { get; set; } = new List<int>();
    public int? WarehouseId { get; set; }
}

public class LoginDto
{
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ChangePasswordDto
{
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class RefreshRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}
