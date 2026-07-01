using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MkWMS.Data.Context;
using MkWMS.Data.Entities;

namespace MkWMS.API.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly MkWMSDbContext _context;
    private readonly IWebHostEnvironment _env;

    public FilesController(MkWMSDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [HttpPost("documents/{documentId}/upload-scan")]
    public async Task<IActionResult> UploadDocumentScan(int documentId, IFormFile file)
    {
        var document = await _context.Documents.FindAsync(documentId);
        if (document == null) return NotFound("Документ не найден");

        if (file == null || file.Length == 0) return BadRequest("Файл не выбран");


        var uploadsFolder = Path.Combine(_env.ContentRootPath, "Scans");
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);


        var uniqueFileName = $"doc_{documentId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        document.FilePath = uniqueFileName;
        await _context.SaveChangesAsync();

        return Ok(new { FilePath = uniqueFileName });
    }

    [HttpGet("documents/{documentId}/download-scan")]
    public async Task<IActionResult> DownloadDocumentScan(int documentId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        if (document == null || string.IsNullOrEmpty(document.FilePath))
            return NotFound("Скан не найден");

        var filePath = Path.Combine(_env.ContentRootPath, "Scans", document.FilePath);
        if (!System.IO.File.Exists(filePath))
            return NotFound("Файл физически отсутствует на сервере");

        var memory = new MemoryStream();
        using (var stream = new FileStream(filePath, FileMode.Open))
        {
            await stream.CopyToAsync(memory);
        }
        memory.Position = 0;


        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var contentType = ext == ".pdf" ? "application/pdf" :
                          (ext == ".png" ? "image/png" : "image/jpeg");

        return File(memory, contentType, document.FilePath);
    }
}