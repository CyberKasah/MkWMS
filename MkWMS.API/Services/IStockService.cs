using MkWMS.Data.Entities;

namespace MkWMS.API.Services;

public interface IStockService
{
    Task<(bool Success, string? Error)> ApplyMovementsAsync(Document document);
    Task ReverseMovementsAsync(int documentId);
}