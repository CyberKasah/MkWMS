using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MkWMS.API.Services;

namespace MkWMS.API.Controllers;

[ApiController]
[Route("api/print")]
[Authorize] // Доступно всем авторизованным пользователям
public class PrintController : ControllerBase
{
    private readonly IPrintService _printService;

    public PrintController(IPrintService printService)
    {
        _printService = printService;
    }

    [HttpGet("torg12/{documentId}")]
    public async Task<IActionResult> PrintTorg12(int documentId)
    {
        var pdf = await _printService.GenerateTorg12Async(documentId);
        return File(pdf, "application/pdf", $"torg12_{documentId}.pdf");
    }

    [HttpGet("upd/{documentId}")]
    public async Task<IActionResult> PrintUpd(int documentId)
    {
        var pdf = await _printService.GenerateUPDAsync(documentId);
        return File(pdf, "application/pdf", $"upd_{documentId}.pdf");
    }

    [HttpGet("inv3/{documentId}")]
    public async Task<IActionResult> PrintInv3(int documentId)
    {
        var pdf = await _printService.GenerateInv3Async(documentId);
        return File(pdf, "application/pdf", $"inv3_{documentId}.pdf");
    }
}