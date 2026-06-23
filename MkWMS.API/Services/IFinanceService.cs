namespace MkWMS.API.Services;

public interface IFinanceService
{
    Task CalculateDocumentCostAsync(int documentId);
    Task<decimal> GetLastPurchasePriceAsync(int productId, int warehouseId);
    Task UpdateDocumentItemsWithRealPricesAsync(int documentId);
    Task UpdateProductPricesFromReceiptAsync(int documentId);
}