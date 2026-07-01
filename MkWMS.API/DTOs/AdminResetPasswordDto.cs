namespace MkWMS.API.DTOs;

public class AdminResetPasswordDto
{
    public string NewPassword { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
}
