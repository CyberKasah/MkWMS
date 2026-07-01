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










    private static decimal GetSignedQuantity(string? documentTypeName, decimal quantity)
    {
        var outgoingTypes = new[] { "Расход", "Списание" };
        bool isOutgoing = documentTypeName != null && outgoingTypes.Contains(documentTypeName);
        return isOutgoing ? -Math.Abs(quantity) : Math.Abs(quantity);
    }

    public async Task<(bool Success, string? Error)> ApplyMovementsAsync(Document document)
    {
        var documentTypeName = document.DocumentType?.Name;

        foreach (var item in document.Items)
        {
            var signedQuantity = GetSignedQuantity(documentTypeName, item.Quantity);




            var balance = await _context.StockBalances
                .FirstOrDefaultAsync(b =>
                    b.ProductId == item.ProductId &&
                    b.WarehouseId == document.WarehouseId &&
                    b.BatchId == item.BatchId &&
                    b.StorageLocationId == item.StorageLocationId);

            if (balance == null)
            {
                balance = new StockBalance
                {
                    ProductId = item.ProductId,
                    WarehouseId = document.WarehouseId,
                    BatchId = item.BatchId,
                    StorageLocationId = item.StorageLocationId,
                    Quantity = 0
                };

                _context.StockBalances.Add(balance);
            }

            var currentQty = balance.Quantity;
            if (currentQty + signedQuantity < 0)
                return (false, $"Недостаточно товара «{item.Product?.Name}» на складе. Требуется {Math.Abs(signedQuantity)}, в наличии {currentQty}.");

            balance.Quantity += signedQuantity;

            _context.StockMovements.Add(new StockMovement
            {
                DocumentId = document.Id,
                ProductId = item.ProductId,
                WarehouseId = document.WarehouseId,
                BatchId = item.BatchId,
                SerialNumberId = item.SerialNumberId,
                StorageLocationId = item.StorageLocationId,
                Price = item.Price,
                QuantityChange = signedQuantity,
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
                    b.BatchId == m.BatchId &&
                    b.StorageLocationId == m.StorageLocationId);



            if (balance != null)
                balance.Quantity -= m.QuantityChange;
        }

        _context.StockMovements.RemoveRange(movements);
    }
}
