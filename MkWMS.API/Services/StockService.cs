using MkWMS.Data.Context;
using MkWMS.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MkWMS.API.Services;

public class StockService : IStockService
{
    private readonly MkWMSDbContext _context;

    public StockService(MkWMSDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string? Error)> ApplyMovementsAsync(Document document)
    {
        foreach (var item in document.Items)
        {
            var balance = await _context.StockBalances
                .FirstOrDefaultAsync(b =>
                    b.ProductId == item.ProductId &&
                    b.WarehouseId == document.WarehouseId &&
                    b.BatchId == item.BatchId);


            if (balance == null)
            {
                balance = new StockBalance
                {
                    ProductId = item.ProductId,
                    WarehouseId = document.WarehouseId,
                    BatchId = item.BatchId,
                    Quantity = 0
                };

                _context.StockBalances.Add(balance);
            }

            if (balance.Quantity + item.Quantity < 0)
                return (false, "Недостаточно остатка");

            balance.Quantity += item.Quantity;

            if (item.Quantity < 0) 
            {
                var currentQty = balance?.Quantity ?? 0;
                if (currentQty + item.Quantity < 0)
                    return (false, $"Недостаточно товара {item.Product?.Name}. Требуется {Math.Abs(item.Quantity)}, в наличии {currentQty}");
            }



            _context.StockMovements.Add(new StockMovement
            {
                DocumentId = document.Id,
                ProductId = item.ProductId,
                WarehouseId = document.WarehouseId,
                BatchId = item.BatchId,
                SerialNumberId = item.SerialNumberId,
                QuantityChange = item.Quantity,
                MovementDate = DateTime.UtcNow
            });
        }

        return (true, null);
    }

    public async Task ReverseMovementsAsync(int documentId)
    {
        var movements = await _context.StockMovements
            .Where(x => x.DocumentId == documentId)
            .ToListAsync();

        foreach (var m in movements)
        {
            var balance = await _context.StockBalances
                .FirstOrDefaultAsync(b =>
                    b.ProductId == m.ProductId &&
                    b.WarehouseId == m.WarehouseId &&
                    b.BatchId == m.BatchId);

            if (balance != null)
                balance.Quantity -= m.QuantityChange;
        }

        _context.StockMovements.RemoveRange(movements);
    }
}