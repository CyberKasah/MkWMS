using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MkWMS.API.DTOs;
using MkWMS.API.Services;
using MkWMS.Data.Context;

namespace MkWMS.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly MkWMSDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IExcelExportService _excelService;

    public ReportsController(MkWMSDbContext context, ICurrentUserService currentUser, IExcelExportService excelService)
    {
        _context = context;
        _currentUser = currentUser;
        _excelService = excelService;

    }

    [HttpGet("stock-balances")]
    public async Task<ActionResult<PagedResult<StockBalanceReportDto>>> GetStockBalancesReport(
        [FromQuery] PagedRequestDto req,
        [FromQuery] int? warehouseId = null,
        [FromQuery] int? productId = null)
    {
        if (req.Page < 1) req.Page = 1;
        if (req.PageSize < 1 || req.PageSize > 100) req.PageSize = 20;

        var query = _context.StockBalances
            .Include(sb => sb.Product)
            .Include(sb => sb.Warehouse)
            .Include(sb => sb.Batch)
            .AsQueryable();

        if (!_currentUser.IsAdmin)
        {
            if (!_currentUser.WarehouseId.HasValue)
                return Forbid("У пользователя не указан склад");

            query = query.Where(sb => sb.WarehouseId == _currentUser.WarehouseId.Value);

            if (warehouseId.HasValue && warehouseId != _currentUser.WarehouseId.Value)
                return Forbid("Нет доступа к указанному складу");
        }

        if (warehouseId.HasValue) query = query.Where(sb => sb.WarehouseId == warehouseId.Value);
        if (productId.HasValue) query = query.Where(sb => sb.ProductId == productId.Value);

        var totalCount = await query.CountAsync();

        var data = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(sb => new StockBalanceReportDto
            {
                Product = sb.Product.Name,
                ProductId = sb.ProductId,
                Warehouse = sb.Warehouse.Name,
                WarehouseId = sb.WarehouseId,
                Batch = sb.Batch != null ? sb.Batch.BatchNumber : null,
                Quantity = sb.Quantity,
                Unit = sb.Product.Unit ?? ""
            })
            .ToListAsync();

        return Ok(new PagedResult<StockBalanceReportDto>
        {
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)req.PageSize),
            Items = data
        });
    }

    [HttpGet("movements")]
    public async Task<ActionResult<PagedResult<StockMovementReportDto>>> GetMovementsReport(
        [FromQuery] PagedRequestDto req,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        if (req.Page < 1) req.Page = 1;
        if (req.PageSize < 1 || req.PageSize > 100) req.PageSize = 20;

        var query = _context.StockMovements
            .Include(sm => sm.Product)
            .Include(sm => sm.Warehouse)
            .Include(sm => sm.Document)
            .AsQueryable();

        if (from.HasValue) query = query.Where(sm => sm.MovementDate >= from);
        if (to.HasValue) query = query.Where(sm => sm.MovementDate <= to);

        if (!_currentUser.IsAdmin && _currentUser.WarehouseId.HasValue)
            query = query.Where(sm => sm.WarehouseId == _currentUser.WarehouseId.Value);

        var totalCount = await query.CountAsync();

        var data = await query
            .OrderByDescending(sm => sm.MovementDate)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(sm => new StockMovementReportDto
            {
                Date = sm.MovementDate,
                Document = sm.Document.DocumentNumber,
                Product = sm.Product.Name,
                Warehouse = sm.Warehouse.Name,
                Quantity = sm.QuantityChange,
                Type = sm.QuantityChange > 0 ? "Приход" : "Расход"
            })
            .ToListAsync();

        return Ok(new PagedResult<StockMovementReportDto>
        {
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)req.PageSize),
            Items = data
        });


    }

    [HttpGet("stock-balances/excel")]
    public async Task<IActionResult> ExportStockBalancesExcel(
    [FromQuery] int? warehouseId = null,
    [FromQuery] int? productId = null)
    {
        // Повторяем вашу логику фильтрации (в идеале её стоит вынести в отдельный метод/сервис)
        var query = _context.StockBalances
            .Include(sb => sb.Product)
            .Include(sb => sb.Warehouse)
            .Include(sb => sb.Batch)
            .AsQueryable();

        // Проверка прав (как в вашем методе)
        if (!_currentUser.IsAdmin)
        {
            if (!_currentUser.WarehouseId.HasValue) return Forbid();
            query = query.Where(sb => sb.WarehouseId == _currentUser.WarehouseId.Value);
        }

        if (warehouseId.HasValue) query = query.Where(sb => sb.WarehouseId == warehouseId.Value);
        if (productId.HasValue) query = query.Where(sb => sb.ProductId == productId.Value);

        var data = await query
            .Select(sb => new StockBalanceReportDto
            {
                Product = sb.Product.Name,
                ProductId = sb.ProductId,
                Warehouse = sb.Warehouse.Name,
                WarehouseId = sb.WarehouseId,
                Batch = sb.Batch != null ? sb.Batch.BatchNumber : "-",
                Quantity = sb.Quantity,
                Unit = sb.Product.Unit ?? ""
            })
            .ToListAsync();

        var fileContent = _excelService.ExportToExcel(data, "Остатки");
        string fileName = $"Stocks_{DateTime.Now:yyyyMMdd}.xlsx";

        return File(
            fileContent,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}