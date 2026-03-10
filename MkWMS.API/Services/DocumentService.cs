using MkWMS.API.DTOs;
using MkWMS.Data.Context;
using MkWMS.Data.Entities;
using MkWMS.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace MkWMS.API.Services;

public class DocumentService : IDocumentService
{
    private readonly MkWMSDbContext _context;
    private readonly IStockService _stockService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;
    private readonly IFinanceService _financeService;   // ← добавлено

    public DocumentService(
        MkWMSDbContext context,
        IStockService stockService,
        IAuditService auditService,
        ICurrentUserService currentUser,
        IFinanceService financeService)   // ← добавлено
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _stockService = stockService ?? throw new ArgumentNullException(nameof(stockService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _financeService = financeService ?? throw new ArgumentNullException(nameof(financeService));
    }

    private async Task<string> GenerateNumberAsync()
    {
        var number = await _context.Database
            .SqlQuery<int>($"SELECT NEXT VALUE FOR DocumentNumberSeq")
            .FirstAsync();

        return $"DOC-{number:D6}";
    }

    public async Task<List<DocumentDto>> GetAllAsync()
    {
        return await _context.Documents
            .Include(d => d.Items)
            .AsNoTracking()
            .Select(d => new DocumentDto
            {
                Id = d.Id,
                DocumentNumber = d.DocumentNumber,
                Status = d.Status.ToString(),
                Comment = d.Comment,
                CreatedDate = d.CreatedDate,
                DocumentTypeId = d.DocumentTypeId,
                WarehouseId = d.WarehouseId,
                DepartmentId = d.DepartmentId,
                CreatedByUserId = d.CreatedByUserId,
                Items = d.Items.Select(i => new DocumentItemDto
                {
                    Id = i.Id,
                    DocumentId = i.DocumentId,
                    ProductId = i.ProductId,
                    BatchId = i.BatchId,
                    SerialNumberId = i.SerialNumberId,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList()
            })
            .ToListAsync();
    }

    public async Task<DocumentDto?> GetByIdAsync(int id)
    {
        var d = await _context.Documents
            .Include(x => x.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (d == null) return null;

        return new DocumentDto
        {
            Id = d.Id,
            DocumentNumber = d.DocumentNumber,
            Status = d.Status.ToString(),
            Comment = d.Comment,
            CreatedDate = d.CreatedDate,
            DocumentTypeId = d.DocumentTypeId,
            WarehouseId = d.WarehouseId,
            DepartmentId = d.DepartmentId,
            CreatedByUserId = d.CreatedByUserId,
            Items = d.Items.Select(i => new DocumentItemDto
            {
                Id = i.Id,
                DocumentId = i.DocumentId,
                ProductId = i.ProductId,
                BatchId = i.BatchId,
                SerialNumberId = i.SerialNumberId,
                Quantity = i.Quantity,
                Price = i.Price
            }).ToList()
        };
    }

    public async Task<int> CreateAsync(CreateDocumentDto dto, int userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        var document = new Document
        {
            DocumentNumber = await GenerateNumberAsync(),
            Status = DocumentStatus.Draft,
            CreatedDate = DateTime.UtcNow,
            DocumentTypeId = dto.DocumentTypeId,
            WarehouseId = dto.WarehouseId,
            CreatedByUserId = userId
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        foreach (var item in dto.Items)
        {
            _context.DocumentItems.Add(new DocumentItem
            {
                DocumentId = document.Id,
                ProductId = item.ProductId,
                BatchId = item.BatchId,
                SerialNumberId = item.SerialNumberId,
                Quantity = item.Quantity,
                Price = null
            });
        }

        await _context.SaveChangesAsync();

        // Расчёт цены после создания строк
        await _financeService.CalculateDocumentCostAsync(document.Id);

        await _auditService.LogAsync(userId, $"Создан документ {document.DocumentNumber}");

        await transaction.CommitAsync();

        return document.Id;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var doc = await _context.Documents
            .Include(d => d.Items)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doc == null) return false;
        if (doc.Status == DocumentStatus.Posted) return false;

        _context.DocumentItems.RemoveRange(doc.Items);
        _context.Documents.Remove(doc);

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string? Error)> PostAsync(int id, int userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
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

            doc.Status = DocumentStatus.Posted;

            // Расчёт цены после проведения
            await _financeService.CalculateDocumentCostAsync(id);

            await _auditService.LogAsync(userId, $"Проведён документ {doc.DocumentNumber}");

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> UnpostAsync(int id, int userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var doc = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doc == null)
                return (false, "Документ не найден");

            if (doc.Status != DocumentStatus.Posted)
                return (false, "Документ не проведён");

            await _stockService.ReverseMovementsAsync(id);

            doc.Status = DocumentStatus.Draft;

            await _auditService.LogAsync(userId, $"Отменено проведение {doc.DocumentNumber}");

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, ex.Message);
        }
    }
}