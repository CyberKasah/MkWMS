using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MkWMS.API.DTOs;
using MkWMS.API.Services;
using MkWMS.Data.Context;

namespace MkWMS.API.Controllers;

[ApiController]
[Route("api/stockbalances")]
[Authorize]
public class StockBalancesController : ControllerBase
{
    private readonly MkWMSDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public StockBalancesController(MkWMSDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<StockBalanceDto>>> GetAll([FromQuery] PagedRequestDto req)
    {
        if (req.Page < 1) req.Page = 1;
        if (req.PageSize < 1 || req.PageSize > 100) req.PageSize = 20;

        var query = _context.StockBalances.AsNoTracking().AsQueryable();

        if (!_currentUser.IsAdmin)
        {
            if (!_currentUser.WarehouseId.HasValue)
                return Forbid("У пользователя не указан склад");

            query = query.Where(x => x.WarehouseId == _currentUser.WarehouseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.ToLower().Trim();
            query = query.Where(x => x.ProductId.ToString().Contains(s));
        }

        query = req.SortBy?.ToLower() switch
        {
            "quantity" => req.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(x => x.Quantity) : query.OrderBy(x => x.Quantity),
            _ => query.OrderBy(x => x.Id)
        };

        var totalCount = await query.CountAsync();

        var data = await query
    .Include(x => x.Product)
    .Include(x => x.StorageLocation)
    .Select(x => new StockBalanceDto
    {
        Id = x.Id,
        ProductId = x.ProductId,
        Quantity = x.Quantity,
        LocationName = x.StorageLocation != null ? x.StorageLocation.Name : "—",
        RfidTag = x.StorageLocation != null ? x.StorageLocation.RfidTag : null
    }).ToListAsync();

        return Ok(new PagedResult<StockBalanceDto>
        {
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)req.PageSize),
            Items = data
        });
    }
}