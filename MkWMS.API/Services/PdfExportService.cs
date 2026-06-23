using Microsoft.EntityFrameworkCore;
using MkWMS.Data.Context;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MkWMS.API.Services;

public class PdfExportService : IPdfExportService
{
    private readonly MkWMSDbContext _context;

    public PdfExportService(MkWMSDbContext context)
    {
        _context = context;
        // Обязательная настройка для QuestPDF
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateDocumentPdfAsync(int documentId)
    {
        // 1. Получаем данные документа со связанными сущностями
        var document = await _context.Documents
            .Include(d => d.DocumentType)
            .Include(d => d.Warehouse)
            // ИСПРАВЛЕНО: используем Items вместо DocumentItems
            .Include(d => d.Items)
                .ThenInclude(di => di.Product)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
            throw new Exception("Документ не найден");

        // 2. Верстаем PDF
        var pdfDocument = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                // Шапка документа
                page.Header().Element(header => ComposeHeader(header, document));

                // Содержимое (таблица с товарами)
                page.Content().Element(content => ComposeContent(content, document));

                // Подвал (подписи, номера страниц)
                page.Footer().Element(ComposeFooter);
            });
        });

        return pdfDocument.GeneratePdf();
    }

    private void ComposeHeader(IContainer container, MkWMS.Data.Entities.Document document)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text($"Накладная № {document.DocumentNumber}")
                    .FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);

                column.Item().Text(text =>
                {
                    text.Span("Дата: ").SemiBold();
                    text.Span($"{document.CreatedDate:dd.MM.yyyy HH:mm}");
                });

                column.Item().Text(text =>
                {
                    text.Span("Тип документа: ").SemiBold();
                    text.Span(document.DocumentType.Name);
                });

                column.Item().Text(text =>
                {
                    text.Span("Склад: ").SemiBold();
                    text.Span(document.Warehouse.Name);
                });
            });
        });
    }

    private void ComposeContent(IContainer container, MkWMS.Data.Entities.Document document)
    {
        container.PaddingVertical(1, Unit.Centimetre).Column(column =>
        {
            column.Spacing(5);

            column.Item().Table(table =>
            {
                // Определение колонок
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);  // №
                    columns.RelativeColumn();    // Товар
                    columns.ConstantColumn(80);  // Кол-во
                    columns.ConstantColumn(80);  // Ед. изм.
                });

                // Шапка таблицы
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("№");
                    header.Cell().Element(CellStyle).Text("Наименование товара");
                    header.Cell().Element(CellStyle).AlignRight().Text("Кол-во");
                    header.Cell().Element(CellStyle).AlignRight().Text("Ед.");

                    static IContainer CellStyle(IContainer c)
                    {
                        return c.DefaultTextStyle(x => x.SemiBold())
                                .PaddingVertical(5)
                                .BorderBottom(1)
                                .BorderColor(Colors.Black);
                    }
                });

                // Строки документа
                int index = 1;
                // ИСПРАВЛЕНО: используем Items вместо DocumentItems
                foreach (var item in document.Items)
                {
                    table.Cell().Element(CellStyle).Text(index.ToString());
                    table.Cell().Element(CellStyle).Text(item.Product.Name);
                    table.Cell().Element(CellStyle).AlignRight().Text(item.Quantity.ToString("0.##"));
                    table.Cell().Element(CellStyle).AlignRight().Text(item.Product.Unit ?? "шт");

                    index++;

                    static IContainer CellStyle(IContainer c)
                    {
                        return c.BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(5);
                    }
                }
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("Сдал: ___________________");
                col.Item().PaddingTop(10).Text("Принял: ___________________");
            });

            row.RelativeItem().AlignRight().Text(x =>
            {
                x.Span("Страница ");
                x.CurrentPageNumber();
                x.Span(" из ");
                x.TotalPages();
            });
        });
    }
}