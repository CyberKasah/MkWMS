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
    private readonly IFinanceService _finance;

    public DocumentPostingService(
        MkWMSDbContext context,
        IStockService stockService,
        IAuditService audit,
        IFinanceService finance)
    {
        _context = context;
        _stockService = stockService;
        _audit = audit;
        _finance = finance;
    }

    public async Task<(bool Success, string? Error)> PostAsync(int id, int userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var doc = await _context.Documents
                .Include(d => d.Items).ThenInclude(i => i.Product)
                .Include(d => d.DocumentType)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doc == null) return (false, "Документ не найден");
            if (doc.Status == DocumentStatus.Posted) return (false, "Документ уже проведён");


            var result = await _stockService.ApplyMovementsAsync(doc);
            if (!result.Success)
            {
                await transaction.RollbackAsync();
                return result;
            }



            if (doc.DocumentType?.Name == "Приход")
            {

                await _finance.UpdateProductPricesFromReceiptAsync(doc.Id);

                await _finance.UpdateDocumentItemsWithRealPricesAsync(doc.Id);
            }


            await _finance.CalculateDocumentCostAsync(doc.Id);


            doc.Status = DocumentStatus.Posted;
            await _audit.LogAsync(userId, $"Проведён документ {doc.DocumentNumber}");

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Ошибка проведения: {ex.Message}");
        }
    }





    public async Task<(bool Success, string? Error)> UnpostAsync(int id, int userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var doc = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id);

            if (doc == null) return (false, "Документ не найден");
            if (doc.Status == DocumentStatus.Cancelled) return (false, "Документ уже отменён");



            if (doc.Status == DocumentStatus.Posted)
            {
                await _stockService.ReverseMovementsAsync(id);
            }

            doc.Status = DocumentStatus.Cancelled;
            await _audit.LogAsync(userId, $"Отменён документ {doc.DocumentNumber}");

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Ошибка отмены документа: {ex.Message}");
        }
    }
}