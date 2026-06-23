using MkWMS.API.DTOs;

namespace MkWMS.API.Services;

public interface IDocumentService
{
    Task<List<DocumentDto>> GetAllAsync();
    Task<DocumentDto?> GetByIdAsync(int id);
    Task<int> CreateAsync(CreateDocumentDto dto, int createdByUserId);
    Task<bool> DeleteAsync(int id);
    Task<(bool Success, string? Error)> PostAsync(int id, int userId);
    Task<(bool Success, string? Error)> UnpostAsync(int id, int userId);
    Task<List<DocumentDto>> GetByBaseIdAsync(int baseDocumentId);
    Task UpdateFilePathAsync(int documentId, string filePath);
    Task<string?> GetFilePathAsync(int documentId);
}
