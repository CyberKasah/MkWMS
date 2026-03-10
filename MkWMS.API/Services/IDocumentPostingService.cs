using MkWMS.API.DTOs;
namespace MkWMS.API.Services
{
    public interface IDocumentPostingService
    {
        Task<(bool Success, string? Error)> PostAsync(int id, int userId);
        Task<(bool Success, string? Error)> UnpostAsync(int id, int userId);
    }
}
