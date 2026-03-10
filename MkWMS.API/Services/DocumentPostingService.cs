using MkWMS.Data.Context;
using MkWMS.Data.Entities;
using MkWMS.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace MkWMS.API.Services;

public class DocumentPostingService : IDocumentPostingService
{
    private readonly MkWMSDbContext _context;
    private readonly IStockService _stockService;
    private readonly IAuditService _audit;
    private readonly IFinanceService _financeService;

    public DocumentPostingService(
        MkWMSDbContext context,
        IStockService stockService,
        IAuditService audit,
        IFinanceService financeService)
    {
        _context = context;
        _stockService = stockService;
        _audit = audit;
        _financeService = financeService;
    }

    public async Task<(bool Success, string? Error)> PostAsync(int id, int userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        var doc = await _context.Documents
            .Include(d => d.Items)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doc == null)
            return (false, "Документ не найден");

        if (doc.Status == DocumentStatus.Posted)
            return (false, "Документ уже проведён");

        var result = await _stockService.ApplyMovementsAsync(doc);

        if (!result.Success)
        {
            await transaction.RollbackAsync();
            return result;
        }

        if (doc.DocumentType?.Name == "Приход")
        {
            await _financeService.UpdateDocumentItemsWithRealPricesAsync(doc.Id);

        }
        doc.Status = DocumentStatus.Posted;

        await _audit.LogAsync(userId, $"Проведен документ {doc.DocumentNumber}");

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UnpostAsync(int id, int userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var doc = await _context.Documents
                .Include(d => d.Items)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doc == null)
                return (false, "Документ не найден");

            if (doc.Status != DocumentStatus.Posted)
                return (false, "Документ не проведён");

            await _stockService.ReverseMovementsAsync(id);

            doc.Status = DocumentStatus.Draft;

            await _audit.LogAsync(userId, $"Отменено проведение документа {doc.DocumentNumber}");

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Ошибка при отмене проводки документа {id}: {ex.Message}");
        }
    }
}