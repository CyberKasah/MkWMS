namespace MkWMS.API.Services;

public interface IPdfExportService
{
    Task<byte[]> GenerateDocumentPdfAsync(int documentId);
}