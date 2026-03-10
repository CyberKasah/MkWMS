using System.Security.Claims;

namespace MkWMS.API.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal User =>
            _httpContextAccessor.HttpContext?.User
            ?? throw new UnauthorizedAccessException("Нет HttpContext");

        public int UserId =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        public string? Login =>
            User.FindFirst(ClaimTypes.Name)?.Value;

        public int? WarehouseId
        {
            get
            {
                var claim = User.FindFirst("warehouseId");
                if (claim == null || string.IsNullOrEmpty(claim.Value))
                    return null;

                return int.Parse(claim.Value);
            }
        }

        public bool IsAdmin =>
            User.Claims.Any(c => c.Type == ClaimTypes.Role &&
                                 (c.Value.Trim().Replace("\"", "").Replace("\n", "") == "Администратор" ||
                                  c.Value.Trim().Replace("\"", "").Replace("\n", "") == "Admin"));
    }
}