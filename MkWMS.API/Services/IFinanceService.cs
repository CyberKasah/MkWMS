using MkWMS.API.DTOs;

namespace MkWMS.API.Services;

public interface IFinanceService
{
    Task CalculateDocumentCostAsync(int documentId);
    Task<decimal> GetLastPurchasePriceAsync(int productId, int warehouseId);
    Task UpdateDocumentItemsWithRealPricesAsync(int documentId);
    Task UpdateProductPricesFromReceiptAsync(int documentId);


    Task<List<StockBalanceReportDto>> GetStockBalanceReportAsync(int? warehouseId = null);
    Task<List<StockMovementReportDto>> GetStockMovementReportAsync(int? warehouseId = null, DateTime? from = null, DateTime? to = null);
}