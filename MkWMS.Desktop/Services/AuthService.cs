using MkWMS.Data.Constants;

namespace MkWMS.Desktop.Services;

public class AuthService
{



    public static AuthService? Current { get; private set; }

    public string? Token { get; private set; }
    public string? RefreshToken { get; private set; }
    public string? Login { get; private set; }
    public bool IsAdministrator { get; private set; }
    public bool IsRukovoditel { get; private set; }
    public bool IsKladovschik { get; private set; }
    public int? WarehouseId { get; private set; }
    public List<string> Roles { get; private set; } = new();

    public AuthService()
    {
        Current = this;
    }

    public void SetUser(string login, List<string> roles, string token, string? refreshToken, int? warehouseId)
    {
        Login = login;
        Roles = roles ?? new();
        Token = token;
        RefreshToken = refreshToken;
        WarehouseId = warehouseId;


        IsAdministrator = Roles.Any(r => string.Equals(r, RoleNames.Admin, StringComparison.OrdinalIgnoreCase));
        IsRukovoditel = Roles.Any(r => string.Equals(r, RoleNames.Manager, StringComparison.OrdinalIgnoreCase));
        IsKladovschik = Roles.Any(r => string.Equals(r, RoleNames.Operator, StringComparison.OrdinalIgnoreCase));
    }



    public void SetTokens(string accessToken, string refreshToken)
    {
        Token = accessToken;
        RefreshToken = refreshToken;
    }

    public void Logout()
    {
        Token = null;
        RefreshToken = null;
        Login = null;
        IsAdministrator = false;
        IsRukovoditel = false;
        IsKladovschik = false;
        WarehouseId = null;
        Roles.Clear();
    }
}