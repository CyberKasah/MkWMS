using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MkWMS.API.DTOs;
using MkWMS.API.Services;
using System.Security.Claims;

namespace MkWMS.API.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentPostingService _postingService;
    private readonly ICurrentUserService _currentUser;   // ← добавлено

    public DocumentsController(
        IDocumentService documentService,
        IDocumentPostingService postingService,
        ICurrentUserService currentUser)                 // ← добавлено
    {
        _documentService = documentService;
        _postingService = postingService;
        _currentUser = currentUser;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException());

    [HttpGet]
    public async Task<ActionResult<PagedResult<DocumentDto>>> GetAll([FromQuery] PagedRequestDto req)
    {
        if (req.Page < 1) req.Page = 1;
        if (req.PageSize < 1 || req.PageSize > 100) req.PageSize = 20;

        var allDocs = await _documentService.GetAllAsync();

        // Фильтр по складу для обычных пользователей
        if (!_currentUser.IsAdmin)
        {
            if (!_currentUser.WarehouseId.HasValue)
                return Forbid("У пользователя не указан склад");

            allDocs = allDocs.Where(d => d.WarehouseId == _currentUser.WarehouseId.Value).ToList();
        }

        // Поиск
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.ToLower().Trim();
            allDocs = allDocs.Where(d =>
                d.DocumentNumber.ToLower().Contains(s) ||
                (d.Comment != null && d.Comment.ToLower().Contains(s))).ToList();
        }

        // Сортировка
        allDocs = req.SortBy?.ToLower() switch
        {
            "number" or "documentnumber" => req.SortDirection?.ToLower() == "desc"
                ? allDocs.OrderByDescending(d => d.DocumentNumber).ToList()
                : allDocs.OrderBy(d => d.DocumentNumber).ToList(),
            "createddate" => req.SortDirection?.ToLower() == "desc"
                ? allDocs.OrderByDescending(d => d.CreatedDate).ToList()
                : allDocs.OrderBy(d => d.CreatedDate).ToList(),
            _ => allDocs.OrderByDescending(d => d.CreatedDate).ToList()
        };

        var totalCount = allDocs.Count;

        var items = allDocs
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToList();

        return Ok(new PagedResult<DocumentDto>
        {
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)req.PageSize),
            Items = items
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var doc = await _documentService.GetByIdAsync(id);
        if (doc == null) return NotFound();
        return Ok(doc);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateDocumentDto dto)
    {
        int createdByUserId = GetUserId();
        var id = await _documentService.CreateAsync(dto, createdByUserId);
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPost("{id}/post")]
    public async Task<IActionResult> Post(int id)
    {
        int userId = GetUserId();
        var result = await _postingService.PostAsync(id, userId);
        if (!result.Success) return BadRequest(result.Error);
        return NoContent();
    }

    [HttpPost("{id}/unpost")]
    public async Task<IActionResult> Unpost(int id)
    {
        int userId = GetUserId();
        var result = await _postingService.UnpostAsync(id, userId);
        if (!result.Success) return BadRequest(result.Error);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _documentService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}