namespace MkWMS.Desktop.Services;

public class AuthService
{
    public string? Token { get; private set; }
    public string? Login { get; private set; }
    public bool IsAdministrator { get; private set; }
    public bool IsRukovoditel { get; private set; }
    public bool IsKladovschik { get; private set; }
    public int? WarehouseId { get; private set; }
    public List<string> Roles { get; private set; } = new();

    public void SetUser(string login, List<string> roles, string token, int? warehouseId)
    {
        Login = login;
        Roles = roles ?? new();
        Token = token;
        WarehouseId = warehouseId;

        // Проверки на роли
        IsAdministrator = Roles.Any(r => string.Equals(r, "Администратор", StringComparison.OrdinalIgnoreCase));
        IsRukovoditel = Roles.Any(r => string.Equals(r, "Руководитель", StringComparison.OrdinalIgnoreCase));
        IsKladovschik = Roles.Any(r => string.Equals(r, "Кладовщик", StringComparison.OrdinalIgnoreCase));
    }

    public void Logout()
    {
        Token = null;
        Login = null;
        IsAdministrator = false;
        IsRukovoditel = false;
        IsKladovschik = false;
        WarehouseId = null;
        Roles.Clear();
    }
}