using MkWMS.Data.Context;
using MkWMS.Data.Entities;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.Reflection;

namespace MkWMS.API.Services;

public class ExcelExportService : IExcelExportService
{
    private readonly MkWMSDbContext _context;
    public ExcelExportService(MkWMSDbContext context)
    {
        _context = context;
    }

    public byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);
        var properties = typeof(T).GetProperties();

        for (int i = 0; i < properties.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = properties[i].Name;
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        var list = data.ToList();
        for (int row = 0; row < list.Count; row++)
        {
            for (int col = 0; col < properties.Length; col++)
            {
                var value = properties[col].GetValue(list[row]);
                worksheet.Cell(row + 2, col + 1).Value = value?.ToString();
            }
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task ImportProductsFromExcelAsync(Stream fileStream)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheets.First();
        var usedRange = worksheet.RangeUsed();
        if (usedRange == null) return;


        var rows = usedRange.RowsUsed();
        var headerRow = rows.First();


        int colArticle = FindColumn(headerRow, "Артикул");
        int colPurchase = FindColumn(headerRow, "Цена закупки");
        int colRetail = FindColumn(headerRow, "Цена розничная");
        int colVat = FindColumn(headerRow, "Ставка НДС");

        if (colArticle == 0) throw new Exception("Колонка 'Артикул' обязательна для импорта.");

        foreach (var row in rows.Skip(1))
        {


            var article = row.Cell(colArticle).GetValue<string>();
            if (string.IsNullOrWhiteSpace(article)) continue;

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Article == article);
            if (product == null) continue;

            if (colPurchase > 0)
                product.PurchasePrice = CleanAndParseDecimal(row.Cell(colPurchase).GetString());

            if (colRetail > 0)
                product.RetailPrice = CleanAndParseDecimal(row.Cell(colRetail).GetString());

            if (colVat > 0)
                product.VatRate = CleanAndParseDecimal(row.Cell(colVat).GetString());
        }
        await _context.SaveChangesAsync();
    }


    private int FindColumn(IXLRangeRow row, string name)
    {
        var cell = row.Cells().FirstOrDefault(c =>
            c.GetString().Trim().Equals(name, StringComparison.OrdinalIgnoreCase));


        return cell?.Address.ColumnNumber ?? 0;
    }

    private decimal CleanAndParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        value = value.Replace(" ", "").Replace("%", "").Replace("р.", "").Replace(".", ",");
        decimal.TryParse(value, out decimal result);
        return result;
    }
}