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

        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateDocumentPdfAsync(int documentId)
    {

        var document = await _context.Documents
            .Include(d => d.DocumentType)
            .Include(d => d.Warehouse)

            .Include(d => d.Items)
                .ThenInclude(di => di.Product)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
            throw new Exception("Документ не найден");


        var pdfDocument = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));


                page.Header().Element(header => ComposeHeader(header, document));


                page.Content().Element(content => ComposeContent(content, document));


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

                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);
                    columns.RelativeColumn();
                    columns.ConstantColumn(80);
                    columns.ConstantColumn(80);
                });


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


                int index = 1;

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