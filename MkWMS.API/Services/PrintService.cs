using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using MkWMS.Data.Context;
using MkWMS.Data.Entities;
using Microsoft.EntityFrameworkCore;
using QuestPDFDocument = QuestPDF.Fluent.Document; // ← alias решает конфликт имён

namespace MkWMS.API.Services;

public class PrintService : IPrintService
{
    private readonly MkWMSDbContext _context;

    public PrintService(MkWMSDbContext context)
    {
        _context = context;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateTorg12Async(int documentId)
    {
        var doc = await LoadDocumentWithDetailsAsync(documentId);

        var pdf = QuestPDFDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Text("ТОРГ-12").FontSize(18).Bold().AlignCenter();

                page.Content().Column(col =>
                {
                    col.Item().Text($"Документ № {doc.DocumentNumber} от {doc.CreatedDate:dd.MM.yyyy}");
                    col.Item().Text($"Склад: {doc.Warehouse?.Name ?? "—"}");

                    if (doc.Counterparty != null)
                        col.Item().Text($"Контрагент: {doc.Counterparty.Name} ИНН {doc.Counterparty.INN ?? "—"}");

                    col.Item().PaddingTop(20).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(40);   // №
                            columns.RelativeColumn(4);    // Наименование
                            columns.ConstantColumn(60);   // Кол-во
                            columns.ConstantColumn(80);   // Цена
                            columns.ConstantColumn(80);   // Сумма
                            columns.ConstantColumn(60);   // НДС
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("№").Bold().AlignCenter();
                            header.Cell().Text("Наименование").Bold();
                            header.Cell().Text("Кол-во").Bold().AlignCenter();
                            header.Cell().Text("Цена").Bold().AlignRight();
                            header.Cell().Text("Сумма").Bold().AlignRight();
                            header.Cell().Text("НДС").Bold().AlignRight();
                        });

                        int rowNumber = 1;
                        foreach (var item in doc.Items)
                        {
                            table.Cell().Text(rowNumber.ToString()).AlignCenter();
                            table.Cell().Text(item.Product?.Name ?? "—");
                            table.Cell().Text(item.Quantity.ToString("N2")).AlignRight();
                            table.Cell().Text(item.Price?.ToString("N2") ?? "0.00").AlignRight();
                            table.Cell().Text(item.Sum.ToString("N2")).AlignRight();
                            table.Cell().Text(item.VatSum.ToString("N2")).AlignRight();
                            rowNumber++;
                        }
                    });

                    var total = doc.Items.Sum(i => i.Sum + i.VatSum);
                    col.Item().PaddingTop(20).AlignRight()
                        .Text($"Итого с НДС: {total:N2} руб.").Bold().FontSize(12);
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Страница ");
                    x.CurrentPageNumber();
                });
            });
        });

        return pdf.GeneratePdf();
    }

    public async Task<byte[]> GenerateUPDAsync(int documentId)
    {
        var doc = await LoadDocumentWithDetailsAsync(documentId);

        var pdf = QuestPDFDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header()
                    .Text("УНИВЕРСАЛЬНЫЙ ПЕРЕДАТОЧНЫЙ ДОКУМЕНТ (УПД)")
                    .SemiBold().FontSize(16).AlignCenter();

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"№ {doc.DocumentNumber} от {doc.CreatedDate:dd.MM.yyyy}");
                        row.RelativeItem().AlignRight().Text("Статус: 1 — счет-фактура + первичный документ");
                    });

                    col.Item().PaddingTop(8).Text($"Поставщик: {doc.Warehouse?.Name ?? "ООО \"Моя Компания\""}");

                    if (doc.Counterparty != null)
                    {
                        col.Item().Text($"Покупатель: {doc.Counterparty.Name}");
                        col.Item().Text($"ИНН/КПП: {doc.Counterparty.INN ?? "—"} / {doc.Counterparty.KPP ?? "—"}");
                    }

                    col.Item().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(35);
                            columns.RelativeColumn(5);
                            columns.ConstantColumn(70);
                            columns.ConstantColumn(70);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(90);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("№").Bold().AlignCenter();
                            header.Cell().Text("Наименование товара (работ, услуг)").Bold();
                            header.Cell().Text("Кол-во").Bold().AlignCenter();
                            header.Cell().Text("Цена без НДС").Bold().AlignRight();
                            header.Cell().Text("НДС %").Bold().AlignCenter();
                            header.Cell().Text("Сумма НДС").Bold().AlignRight();
                            header.Cell().Text("Всего с НДС").Bold().AlignRight();
                        });

                        int rowNumber = 1;
                        foreach (var item in doc.Items)
                        {
                            var vatRate = item.Product?.VatRate ?? 22;
                            var price = item.Price.GetValueOrDefault();
                            var priceWithoutVat = vatRate > 0 ? price / (1 + vatRate / 100m) : price;

                            table.Cell().Text(rowNumber.ToString()).AlignCenter();
                            table.Cell().Text(item.Product?.Name ?? "—");
                            table.Cell().Text(item.Quantity.ToString("N3")).AlignCenter();
                            table.Cell().Text(priceWithoutVat.ToString("N2")).AlignRight();
                            table.Cell().Text($"{vatRate}%").AlignCenter();
                            table.Cell().Text(item.VatSum.ToString("N2")).AlignRight();
                            table.Cell().Text(item.Sum.ToString("N2")).AlignRight();

                            rowNumber++;
                        }
                    });

                    var totalWithoutVat = doc.Items.Sum(i =>
                    {
                        var rate = i.Product?.VatRate ?? 22;
                        return rate > 0
                            ? i.Price.GetValueOrDefault() * i.Quantity * (100m / (100m + rate))
                            : i.Price.GetValueOrDefault() * i.Quantity;
                    });

                    var totalVat = doc.Items.Sum(i => i.VatSum);
                    var grandTotal = totalWithoutVat + totalVat;

                    col.Item().PaddingTop(15).AlignRight().Column(summary =>
                    {
                        summary.Item().Text($"Всего к оплате: {grandTotal:N2} руб.").Bold().FontSize(12);
                        summary.Item().Text($"в т.ч. НДС: {totalVat:N2} руб.");
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Страница ");
                    x.CurrentPageNumber();
                    x.Span(" из ");
                    x.TotalPages();
                });
            });
        });

        return pdf.GeneratePdf();
    }

    public async Task<byte[]> GenerateInv3Async(int documentId)
    {
        var doc = await _context.Documents
            .Include(d => d.Items).ThenInclude(i => i.Product)
            .Include(d => d.Warehouse)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (doc == null) throw new Exception("Документ не найден");

        // Реальные остатки по складу и товарам документа
        var productIds = doc.Items.Select(i => i.ProductId).Distinct().ToList();
        var balances = await _context.StockBalances
            .Where(sb => sb.WarehouseId == doc.WarehouseId && productIds.Contains(sb.ProductId))
            .ToDictionaryAsync(
                sb => sb.ProductId,
                sb => sb.Quantity);

        var pdf = QuestPDFDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(35);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                page.Header()
                    .Text("ИНВЕНТАРИЗАЦИОННАЯ ОПИСЬ ТОВАРНО-МАТЕРИАЛЬНЫХ ЦЕННОСТЕЙ (ИНВ-3)")
                    .SemiBold().FontSize(14).AlignCenter();

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"№ {doc.DocumentNumber} от {doc.CreatedDate:dd.MM.yyyy}");
                        row.RelativeItem().AlignRight().Text($"Склад: {doc.Warehouse?.Name ?? "—"}");
                    });

                    col.Item().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(35);
                            columns.RelativeColumn(6);
                            columns.ConstantColumn(50);
                            columns.ConstantColumn(45);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(70);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("№ п/п").Bold().AlignCenter();
                            header.Cell().Text("Наименование товарно-материальных ценностей").Bold();
                            header.Cell().Text("Код").Bold().AlignCenter();
                            header.Cell().Text("Ед. изм.").Bold().AlignCenter();
                            header.Cell().Text("По данным учета").Bold().AlignCenter();
                            header.Cell().Text("Фактическое наличие").Bold().AlignCenter();
                            header.Cell().Text("Отклонение").Bold().AlignCenter();
                        });

                        int rowNumber = 1;
                        foreach (var item in doc.Items)
                        {
                            var accountingQty = item.Quantity;
                            var actualQty = balances.TryGetValue(item.ProductId, out var qty) ? qty : 0m;
                            var difference = actualQty - accountingQty;

                            table.Cell().Text(rowNumber.ToString()).AlignCenter();
                            table.Cell().Text(item.Product?.Name ?? "—");
                            table.Cell().Text(item.Product?.Article ?? "—").AlignCenter();
                            table.Cell().Text(item.Product?.Unit ?? "шт").AlignCenter();
                            table.Cell().Text(accountingQty.ToString("N3")).AlignRight();
                            table.Cell().Text(actualQty.ToString("N3")).AlignRight();
                            table.Cell()
                                .Text(difference.ToString("N3"))
                                .AlignRight()
                                .FontColor(difference > 0 ? Colors.Green.Medium :
                                           difference < 0 ? Colors.Red.Medium : Colors.Grey.Medium);

                            rowNumber++;
                        }
                    });

                    col.Item().PaddingTop(20).AlignRight()
                        .Text($"Всего позиций: {doc.Items.Count}").Bold();
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Страница ");
                    x.CurrentPageNumber();
                    x.Span(" из ");
                    x.TotalPages();
                });
            });
        });

        return pdf.GeneratePdf();
    }

    private async Task<MkWMS.Data.Entities.Document> LoadDocumentWithDetailsAsync(int documentId)
    {
        var doc = await _context.Documents
            .Include(d => d.Items).ThenInclude(i => i.Product)
            .Include(d => d.Counterparty)
            .Include(d => d.Warehouse)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        return doc ?? throw new Exception("Документ не найден");
    }
}