public interface IExcelExportService
{
    byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName);

    Task ImportProductsFromExcelAsync(Stream fileStream);
}