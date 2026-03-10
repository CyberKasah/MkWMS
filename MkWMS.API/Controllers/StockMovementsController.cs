using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MkWMS.API.DTOs;
using MkWMS.API.Services;
using MkWMS.Data.Context;
using MkWMS.Data.Entities;

namespace MkWMS.API.Controllers;

[ApiController]
[Route("api/stockmovements")]
[Authorize]
public class StockMovementsController : ControllerBase
{
    private readonly MkWMSDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public StockMovementsController(MkWMSDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<StockMovementDto>>> GetAll([FromQuery] PagedRequestDto req)
    {
        if (req.Page < 1) req.Page = 1;
        if (req.PageSize < 1 || req.PageSize > 100) req.PageSize = 20;

        var query = _context.StockMovements.AsNoTracking().AsQueryable();

        if (!_currentUser.IsAdmin && _currentUser.WarehouseId.HasValue)
            query = query.Where(x => x.WarehouseId == _currentUser.WarehouseId.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.ToLower().Trim();
            query = query.Where(x =>
                x.ProductId.ToString().Contains(s) ||
                x.DocumentId.ToString().Contains(s));
        }

        query = req.SortBy?.ToLower() switch
        {
            "date" or "movementdate" => req.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(x => x.MovementDate) : query.OrderBy(x => x.MovementDate),
            _ => query.OrderByDescending(x => x.MovementDate)
        };

        var totalCount = await query.CountAsync();

        var data = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(x => new StockMovementDto
            {
                Id = x.Id,
                DocumentId = x.DocumentId,
                ProductId = x.ProductId,
                WarehouseId = x.WarehouseId,
                BatchId = x.BatchId,
                SerialNumberId = x.SerialNumberId,
                QuantityChange = x.QuantityChange,
                MovementDate = x.MovementDate
            }).ToListAsync();

        return Ok(new PagedResult<StockMovementDto>
        {
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)req.PageSize),
            Items = data
        });
    }

    [HttpPost]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> Create(StockMovementDto dto)
    {
        var entity = new StockMovement
        {
            DocumentId = dto.DocumentId,
            ProductId = dto.ProductId,
            WarehouseId = dto.WarehouseId,
            BatchId = dto.BatchId,
            SerialNumberId = dto.SerialNumberId,
            QuantityChange = dto.QuantityChange,
            MovementDate = DateTime.UtcNow
        };

        _context.StockMovements.Add(entity);
        await _context.SaveChangesAsync();

        dto.Id = entity.Id;
        return Ok(dto);
    }
}