using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MkWMS.API.DTOs;
using MkWMS.Data.Context;

namespace MkWMS.API.Controllers;

[ApiController]
[Route("api/auditlogs")]
[Authorize(Policy = "AdminPolicy")]
public class AuditLogsController : ControllerBase
{
    private readonly MkWMSDbContext _context;

    public AuditLogsController(MkWMSDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetAll([FromQuery] PagedRequestDto req)
    {
        if (req.Page < 1) req.Page = 1;
        if (req.PageSize < 1 || req.PageSize > 100) req.PageSize = 20;

        var query = _context.AuditLogs
            .Include(x => x.User)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.ToLower().Trim();
            query = query.Where(x =>
                x.Action.ToLower().Contains(s) ||
                (x.EntityName != null && x.EntityName.ToLower().Contains(s)) ||
                (x.User.Login != null && x.User.Login.ToLower().Contains(s)) ||
                (x.User.FullName != null && x.User.FullName.ToLower().Contains(s)));
        }

        query = req.SortBy?.ToLower() switch
        {
            "action" => req.SortDirection?.ToLower() == "desc"
                ? query.OrderByDescending(x => x.Action)
                : query.OrderBy(x => x.Action),
            "date" => req.SortDirection?.ToLower() == "desc"
                ? query.OrderByDescending(x => x.ActionDate)
                : query.OrderBy(x => x.ActionDate),
            _ => query.OrderByDescending(x => x.ActionDate)
        };

        var totalCount = await query.CountAsync();

        var data = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(x => new AuditLogDto
            {
                Id = x.Id,
                UserId = x.UserId,
                UserLogin = x.User.Login,
                UserFullName = x.User.FullName ?? x.User.Login ?? "Неизвестный пользователь",
                Action = x.Action,
                ActionDate = x.ActionDate,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                ChangesJson = x.ChangesJson
            })
            .ToListAsync();

        return Ok(new PagedResult<AuditLogDto>
        {
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)req.PageSize),
            Items = data
        });
    }
}