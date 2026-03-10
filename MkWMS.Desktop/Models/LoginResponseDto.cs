using MkWMS.API.DTOs;
namespace MkWMS.Desktop.Models;

public class LoginResponseDto
{
    public string? Token { get; set; }
    public bool RequiresPasswordChange { get; set; }
    public string? Message { get; set; }
    public UserDto? User { get; set; }   
}