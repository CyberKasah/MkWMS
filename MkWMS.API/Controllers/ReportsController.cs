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
    private readonly IFinanceService _financeService;

    public ReportsController(MkWMSDbContext context, ICurrentUserService currentUser, IExcelExportService excelService, IFinanceService financeService)
    {
        _context = context;
        _currentUser = currentUser;
        _excelService = excelService;
        _financeService = financeService;
    }








    private (bool Forbidden, int? WarehouseId) ResolveWarehouseFilter(int? requestedWarehouseId)
    {
        if (_currentUser.CanSeeAllWarehouses)
            return (false, requestedWarehouseId);

        if (!_currentUser.WarehouseId.HasValue)
            return (true, null);

        if (requestedWarehouseId.HasValue && requestedWarehouseId != _currentUser.WarehouseId.Value)
            return (true, null);

        return (false, _currentUser.WarehouseId.Value);
    }

    [HttpGet("stock-balances")]
    public async Task<ActionResult<PagedResult<StockBalanceReportDto>>> GetStockBalancesReport(
        [FromQuery] PagedRequestDto req,
        [FromQuery] int? warehouseId = null,
        [FromQuery] int? productId = null)
    {
        if (req.Page < 1) req.Page = 1;
        if (req.PageSize < 1 || req.PageSize > 100) req.PageSize = 20;

        var (forbidden, effectiveWarehouseId) = ResolveWarehouseFilter(warehouseId);
        if (forbidden) return Forbid("Нет доступа к указанному складу");

        var all = await _financeService.GetStockBalanceReportAsync(effectiveWarehouseId);
        if (productId.HasValue)
            all = all.Where(x => x.ProductId == productId.Value).ToList();

        var totalCount = all.Count;
        var data = all
            .OrderBy(x => x.Product)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToList();

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

        var (forbidden, effectiveWarehouseId) = ResolveWarehouseFilter(null);
        if (forbidden) return Forbid("Нет доступа к указанному складу");

        var all = await _financeService.GetStockMovementReportAsync(effectiveWarehouseId, from, to);
        var totalCount = all.Count;
        var data = all
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToList();

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
        var (forbidden, effectiveWarehouseId) = ResolveWarehouseFilter(warehouseId);
        if (forbidden) return Forbid();

        var data = await _financeService.GetStockBalanceReportAsync(effectiveWarehouseId);
        if (productId.HasValue)
            data = data.Where(x => x.ProductId == productId.Value).ToList();

        var fileContent = _excelService.ExportToExcel(data, "Остатки");
        string fileName = $"Stocks_{DateTime.Now:yyyyMMdd}.xlsx";

        return File(
            fileContent,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }



    [HttpGet("movements/excel")]
    public async Task<IActionResult> ExportMovementsExcel(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var (forbidden, effectiveWarehouseId) = ResolveWarehouseFilter(null);
        if (forbidden) return Forbid();

        var data = await _financeService.GetStockMovementReportAsync(effectiveWarehouseId, from, to);
        var fileContent = _excelService.ExportToExcel(data, "Движения");
        string fileName = $"Movements_{DateTime.Now:yyyyMMdd}.xlsx";

        return File(
            fileContent,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}
