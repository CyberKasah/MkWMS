public interface IPrintService
{
    Task<byte[]> GenerateTorg12Async(int documentId);   // ТОРГ-12
    Task<byte[]> GenerateUPDAsync(int documentId);     // УПД (упрощённо)
    Task<byte[]> GenerateInv3Async(int documentId);    // ИНВ-3
}