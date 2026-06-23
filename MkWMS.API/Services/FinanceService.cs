using Microsoft.EntityFrameworkCore;
using MkWMS.API.DTOs;
using MkWMS.Data.Context;
using MkWMS.Data.Entities;

namespace MkWMS.API.Services;

public class FinanceService : IFinanceService
{
    private readonly MkWMSDbContext _context;

    public FinanceService(MkWMSDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> GetLastPurchasePriceAsync(int productId, int warehouseId)
    {
        var lastMovement = await _context.StockMovements
            .Where(m => m.ProductId == productId &&
                        m.WarehouseId == warehouseId &&
                        m.QuantityChange > 0)
            .OrderByDescending(m => m.MovementDate)
            .FirstOrDefaultAsync();

        if (lastMovement == null) return 0m;

        var documentItem = await _context.DocumentItems
            .FirstOrDefaultAsync(di => di.DocumentId == lastMovement.DocumentId &&
                                       di.ProductId == productId);

        return documentItem?.Price ?? 0m;
    }

    public async Task UpdateDocumentItemsWithRealPricesAsync(int documentId)
    {
        var doc = await _context.Documents
            .Include(d => d.DocumentType)
            .Include(d => d.Items)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (doc == null) return;

        // Для Прихода и Инвентаризации цену НЕ перезаписываем (оставляем введённую пользователем)
        if (doc.DocumentType.Name == "Приход" || doc.DocumentType.Name == "Инвентаризация")
            return;

        int warehouseId = doc.WarehouseId;
        foreach (var item in doc.Items)
        {
            var lastPrice = await GetLastPurchasePriceAsync(item.ProductId, warehouseId);
            if (lastPrice > 0)
                item.Price = lastPrice;
        }
    }

    /// <summary>
    /// Главный метод: обновляет цену (где нужно) + ВСЕГДА пересчитывает Сумму НДС
    /// </summary>
    public async Task CalculateDocumentCostAsync(int documentId)
    {
        var doc = await _context.Documents
            .Include(d => d.Items)
                .ThenInclude(i => i.Product)   // ← ОБЯЗАТЕЛЬНО для получения VatRate
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (doc == null || !doc.Items.Any()) return;

        // 1. Обновляем закупочные цены (если нужно)
        await UpdateDocumentItemsWithRealPricesAsync(documentId);

        // 2. Пересчитываем НДС для каждой строки
        foreach (var item in doc.Items)
        {
            var price = item.Price ?? 0m;
            var rate = item.Product?.VatRate ?? 0m;   // берём ставку из товара

            item.VatSum = price * item.Quantity * (rate / 100m);
        }

        await _context.SaveChangesAsync();
    }
    /// <summary>
    /// Заполняет отчёт по остаткам с НДС
    /// </summary>
    public async Task<List<StockBalanceReportDto>> GetStockBalanceReportAsync(int? warehouseId = null)
    {
        var query = _context.StockBalances
            .Include(sb => sb.Product)
            .Include(sb => sb.Warehouse)
            .Include(sb => sb.Batch)
            .AsNoTracking()
            .AsQueryable();

        if (warehouseId.HasValue)
            query = query.Where(sb => sb.WarehouseId == warehouseId.Value);

        return await query
            .Select(sb => new StockBalanceReportDto
            {
                ProductId = sb.ProductId,
                Product = sb.Product.Name,
                WarehouseId = sb.WarehouseId,
                Warehouse = sb.Warehouse.Name,
                Batch = sb.Batch != null ? sb.Batch.BatchNumber : null,
                Quantity = sb.Quantity,
                Unit = sb.Product.Unit ?? "шт",

                PurchasePrice = sb.Product.PurchasePrice,
                VatRate = sb.Product.VatRate,
                TotalValue = sb.Quantity * sb.Product.PurchasePrice,
                TotalVat = sb.Quantity * sb.Product.PurchasePrice * (sb.Product.VatRate / 100m)
            })
            .ToListAsync();
    }

    /// <summary>
    /// Заполняет отчёт по движениям с НДС (без ?. и ?? в лямбде)
    /// </summary>
    public async Task<List<StockMovementReportDto>> GetStockMovementReportAsync(
     int? warehouseId = null,
     DateTime? from = null,
     DateTime? to = null)
    {
        var query = _context.StockMovements
            .Include(sm => sm.Product)
            .Include(sm => sm.Warehouse)
            .Include(sm => sm.Document)
                .ThenInclude(d => d.DocumentType) // <-- ВАЖНО: Подтягиваем тип документа
            .Include(sm => sm.StorageLocation)
            .AsNoTracking()
            .AsQueryable();

        if (warehouseId.HasValue) query = query.Where(sm => sm.WarehouseId == warehouseId.Value);
        if (from.HasValue) query = query.Where(sm => sm.MovementDate >= from.Value);
        if (to.HasValue) query = query.Where(sm => sm.MovementDate <= to.Value);

        return await query
            .Select(sm => new StockMovementReportDto
            {
                Date = sm.MovementDate,
                Document = sm.Document.DocumentNumber,
                Product = sm.Product.Name,
                Warehouse = sm.Warehouse.Name,
                Quantity = sm.QuantityChange,
                // Если есть тип документа - берем его, иначе fallback на плюс/минус
                Type = sm.Document.DocumentType != null ? sm.Document.DocumentType.Name : (sm.QuantityChange > 0 ? "Приход" : "Расход"),
                LocationName = sm.StorageLocation != null ? (sm.StorageLocation.Name ?? "") : "",
                RfidTag = sm.StorageLocation != null ? (sm.StorageLocation.RfidTag ?? "") : "",
                Price = sm.Price ?? 0m,
                VatRate = sm.Product.VatRate,
                // Для отчетов берем количество по модулю (чтобы не было отрицательных сумм списания)
                VatSum = (sm.Price ?? 0m) * Math.Abs(sm.QuantityChange) * (sm.Product.VatRate / 100m),
                TotalValue = (sm.Price ?? 0m) * Math.Abs(sm.QuantityChange)
            })
            .OrderByDescending(sm => sm.Date)
            .ToListAsync();
    }
    public async Task UpdateProductPricesFromReceiptAsync(int documentId)
    {
        var doc = await _context.Documents
            .Include(d => d.DocumentType)
            .Include(d => d.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (doc == null || doc.DocumentType.Name != "Приход") return;

        foreach (var item in doc.Items)
        {
            if (item.Price.HasValue && item.Price.Value > 0)
            {
                // Обновляем закупочную цену
                item.Product.PurchasePrice = item.Price.Value;

                // Опционально: автоматическая наценка для розницы (например, +30%).
                // Если розница задается вручную, эту строку можно убрать.
                item.Product.RetailPrice = item.Price.Value * 1.30m;
            }
        }
        await _context.SaveChangesAsync();
    }

}