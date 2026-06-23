using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MkWMS.API.Services;

namespace MkWMS.API.Controllers;

[ApiController]
[Route("api/excel")]
[Authorize(Policy = "AdminPolicy")]
public class ExcelController : ControllerBase
{
    private readonly IExcelExportService _excelService;

    public ExcelController(IExcelExportService excelService)
    {
        _excelService = excelService;
    }

    [HttpPost("import-products")]
    public async Task<IActionResult> ImportProducts(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("Файл пуст");

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        await _excelService.ImportProductsFromExcelAsync(stream);

        return Ok("Импорт успешно завершен");
    }
}