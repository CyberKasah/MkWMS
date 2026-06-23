public interface IExcelExportService
{
    byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName);
    // НОВЫЙ МЕТОД:
    Task ImportProductsFromExcelAsync(Stream fileStream);
}