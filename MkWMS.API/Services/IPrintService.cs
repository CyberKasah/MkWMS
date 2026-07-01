public interface IPrintService
{
    Task<byte[]> GenerateTorg12Async(int documentId);
    Task<byte[]> GenerateUPDAsync(int documentId);
    Task<byte[]> GenerateInv3Async(int documentId);
}