using System.Security.Claims;
namespace MkWMS.API.Services
{
    public interface ICurrentUserService
    {
        int UserId { get; }
        int? WarehouseId { get; }
        bool IsAdmin { get; }


        bool IsManager { get; }
        bool CanSeeAllWarehouses { get; }
        string? Login { get; }
    }
}
