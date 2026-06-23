using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MkWMS.API.DTOs;
using MkWMS.Data.Context;
using MkWMS.Data.Entities;
using MkWMS.Data.Enums;

namespace MkWMS.API.Services;

public class DocumentService : IDocumentService
{
    private readonly MkWMSDbContext _context;
    private readonly IStockService _stockService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;
    private readonly IFinanceService _financeService;

    public DocumentService(
        MkWMSDbContext context,
        IStockService stockService,
        IAuditService auditService,
        ICurrentUserService currentUser,
        IFinanceService financeService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _stockService = stockService ?? throw new ArgumentNullException(nameof(stockService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _financeService = financeService ?? throw new ArgumentNullException(nameof(financeService));
    }

    public async Task<int> CreateAsync(CreateDocumentDto dto, int userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. КОНТРОЛЬ ДОКУМЕНТА-ОСНОВАНИЯ
            if (dto.BaseDocumentId.HasValue)
            {
                var baseDoc = await _context.Documents
                    .Include(d => d.Items)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == dto.BaseDocumentId.Value);

                if (baseDoc != null)
                {
                    foreach (var item in dto.Items)
                    {
                        var baseItemQty = baseDoc.Items
                            .Where(i => i.ProductId == item.ProductId)
                            .Sum(i => i.Quantity);

                        if (item.Quantity > baseItemQty)
                        {
                            throw new InvalidOperationException(
                                $"Количество товара ID {item.ProductId} ({item.Quantity}) " +
                                $"превышает доступное количество в документе-основании ({baseItemQty}).");
                        }
                    }
                }
            }

            // 2. ГЕНЕРАЦИЯ НОМЕРА
            int seqValue;
            var connection = _context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction.GetDbTransaction();
                command.CommandText = "SELECT NEXT VALUE FOR DocumentNumberSeq";
                var result = await command.ExecuteScalarAsync();
                seqValue = Convert.ToInt32(result);
            }
            string generatedNumber = $"DOC-{seqValue:D6}";

            // ИСПРАВЛЕНИЕ: Надежная проверка номера
            string docNumber = string.IsNullOrWhiteSpace(dto.Number) ? generatedNumber : dto.Number;

            // 3. СОЗДАНИЕ ЗАГОЛОВКА
            var document = new Document
            {
                DocumentNumber = docNumber,
                Status = DocumentStatus.Draft,
                CreatedDate = DateTime.UtcNow,
                DocumentTypeId = dto.DocumentTypeId,
                WarehouseId = dto.WarehouseId,
                CreatedByUserId = userId,
                BaseDocumentId = dto.BaseDocumentId,
                CounterpartyId = dto.CounterpartyId,
                ExternalNumber = dto.ExternalNumber, 
                ExternalDate = dto.ExternalDate,
                Comment = dto.Comment
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            // 4. СОЗДАНИЕ СТРОК
            if(dto.Items != null && dto.Items.Any())
        {
                var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id);

                var itemsToAdd = dto.Items.Select(itemDto =>
                {
                    if (!products.TryGetValue(itemDto.ProductId, out var product))
                        throw new Exception($"Товар с Id {itemDto.ProductId} не найден.");

                    // ИСПРАВЛЕНИЕ: Если цену не ввели (> 0), берем закупочную цену из базы
                    decimal finalPrice = itemDto.Price > 0 ? itemDto.Price : product.PurchasePrice;
                    decimal vatSum = finalPrice * itemDto.Quantity * (product.VatRate / 100m);

                    return new DocumentItem
                    {
                        DocumentId = document.Id,
                        ProductId = itemDto.ProductId,
                        BatchId = itemDto.BatchId <= 0 ? null : itemDto.BatchId,
                        SerialNumberId = itemDto.SerialNumberId <= 0 ? null : itemDto.SerialNumberId,
                        Quantity = itemDto.Quantity,
                        Price = finalPrice, // Присваиваем рассчитанную цену
                        VatSum = vatSum     // Присваиваем рассчитанный НДС
                    };
                }).ToList();

                _context.DocumentItems.AddRange(itemsToAdd);
                await _context.SaveChangesAsync();
            }

            if (_financeService != null) await _financeService.CalculateDocumentCostAsync(document.Id);
            if (_auditService != null) await _auditService.LogAsync(userId, $"Создан документ {document.DocumentNumber}");

            await transaction.CommitAsync();
            return document.Id;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    public async Task<List<DocumentDto>> GetAllAsync()
    {
        return await _context.Documents
            .Include(d => d.DocumentType)
            .Include(d => d.Warehouse)
            .Include(d => d.Counterparty)
            .Include(d => d.Items)
                .ThenInclude(i => i.Product)
            .AsNoTracking()
            .OrderByDescending(d => d.CreatedDate)
            .Select(d => new DocumentDto
            {
                Id = d.Id,
                DocumentNumber = d.DocumentNumber,
                Status = d.Status.ToString(),
                Comment = d.Comment,
                CreatedDate = d.CreatedDate,
                DocumentTypeId = d.DocumentTypeId,
                DocumentTypeName = d.DocumentType.Name,
                WarehouseId = d.WarehouseId,
                WarehouseName = d.Warehouse.Name,
                CounterpartyId = d.CounterpartyId,
                CounterpartyName = d.Counterparty != null ? d.Counterparty.Name : "—",
                DepartmentId = d.DepartmentId,
                CreatedByUserId = d.CreatedByUserId,
                ExternalNumber = d.ExternalNumber,
                Items = d.Items.Select(i => new DocumentItemDto
                {
                    Id = i.Id,
                    DocumentId = i.DocumentId,
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    BatchId = i.BatchId,
                    SerialNumberId = i.SerialNumberId,
                    Quantity = i.Quantity,
                    Price = i.Price,
                    VatSum = i.VatSum
                }).ToList()
            })
            .ToListAsync();
    }

    public async Task<DocumentDto?> GetByIdAsync(int id)
    {
        var d = await _context.Documents
            .Include(x => x.DocumentType)
            .Include(x => x.Warehouse)
            .Include(x => x.Counterparty)
            .Include(x => x.Items)
                .ThenInclude(i => i.Product)
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
            DocumentTypeName = d.DocumentType?.Name ?? "—",
            WarehouseId = d.WarehouseId,
            WarehouseName = d.Warehouse?.Name ?? "—",
            CounterpartyId = d.CounterpartyId,
            CounterpartyName = d.Counterparty?.Name ?? "—",
            DepartmentId = d.DepartmentId,
            CreatedByUserId = d.CreatedByUserId,
            ExternalDate = d.ExternalDate,
            Items = d.Items.Select(i => new DocumentItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? "Неизвестный товар",
                Quantity = i.Quantity,
                Price = i.Price,
                VatSum = i.VatSum
            }).ToList()
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var doc = await _context.Documents.Include(d => d.Items).FirstOrDefaultAsync(d => d.Id == id);
        if (doc == null || doc.Status == DocumentStatus.Posted) return false;

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
            var doc = await _context.Documents.Include(d => d.Items).FirstOrDefaultAsync(d => d.Id == id);
            if (doc == null) return (false, "Документ не найден");
            if (doc.Status == DocumentStatus.Posted) return (false, "Документ уже проведён");

            var result = await _stockService.ApplyMovementsAsync(doc);
            if (!result.Success)
            {
                await transaction.RollbackAsync();
                return result;
            }

            doc.Status = DocumentStatus.Posted;
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
            var doc = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id);
            if (doc == null) return (false, "Документ не найден");
            if (doc.Status != DocumentStatus.Posted) return (false, "Документ не проведён");

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

    public async Task UpdateFilePathAsync(int documentId, string filePath)
    {
        var doc = await _context.Documents.FindAsync(documentId);
        if (doc != null) { doc.FilePath = filePath; await _context.SaveChangesAsync(); }
    }

    public async Task<string?> GetFilePathAsync(int documentId)
    {
        return (await _context.Documents.FindAsync(documentId))?.FilePath;
    }

    public async Task<List<DocumentDto>> GetByBaseIdAsync(int baseDocumentId)
    {
        return await _context.Documents
            .Where(d => d.BaseDocumentId == baseDocumentId)
            .AsNoTracking()
            .Select(d => new DocumentDto
            {
                Id = d.Id,
                DocumentNumber = d.DocumentNumber,
                Status = d.Status.ToString(),
                CreatedDate = d.CreatedDate,
                DocumentTypeId = d.DocumentTypeId
            })
            .ToListAsync();
    }
}