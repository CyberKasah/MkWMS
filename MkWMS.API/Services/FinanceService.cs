using MkWMS.Data.Context;
using Microsoft.EntityFrameworkCore;
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

        if (lastMovement == null)
        {
            return 0m;
        }
        var documentItem = await _context.DocumentItems
            .FirstOrDefaultAsync(di => di.DocumentId == lastMovement.DocumentId &&
                                       di.ProductId == productId);

        return documentItem?.Price ?? 0m;
    }
    public async Task UpdateDocumentItemsWithRealPricesAsync(int documentId)
    {
        var doc = await _context.Documents
            .Include(d => d.Items)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (doc == null)
            return;
        int warehouseId = doc.WarehouseId;

        foreach (var item in doc.Items)
        {
            var lastPrice = await GetLastPurchasePriceAsync(item.ProductId, warehouseId);
            item.Price = lastPrice > 0 ? lastPrice : item.Price;
        }

        await _context.SaveChangesAsync();
    }

    public async Task CalculateDocumentCostAsync(int documentId)
    {
        await UpdateDocumentItemsWithRealPricesAsync(documentId);
    }
}