using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MkWMS.API.DTOs;
using MkWMS.Data.Context;
using MkWMS.Data.Entities;

namespace MkWMS.API.Controllers;

[ApiController]
[Route("api/serialnumbers")]
[Authorize]
public class SerialNumbersController : ControllerBase
{
    private readonly MkWMSDbContext _context;

    public SerialNumbersController(MkWMSDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<SerialNumberDto>>> GetAll([FromQuery] PagedRequestDto req)
    {
        try
        {

            var page = req.Page < 1 ? 1 : req.Page;
            var pageSize = req.PageSize < 1 ? 20 : req.PageSize;

            var query = _context.SerialNumbers.AsNoTracking().AsQueryable();


            if (!string.IsNullOrWhiteSpace(req.Search))
            {
                var s = req.Search.ToLower().Trim();
                query = query.Where(x => x.Number.ToLower().Contains(s));
            }


            var totalCount = await query.CountAsync();


            var data = await query
        .OrderByDescending(x => x.Id)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(x => new SerialNumberDto
        {
            Id = x.Id,
            Number = x.Number,
            Status = x.Status,
            ProductId = x.ProductId,
            ProductName = x.Product != null ? x.Product.Name : "Неизвестно",
            RfidTag = x.RfidTag,
            DataMatrix = x.DataMatrix
        }).ToListAsync();

            return Ok(new PagedResult<SerialNumberDto>
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Items = data ?? new List<SerialNumberDto>()
            });
        }
        catch (Exception ex)
        {

            Console.WriteLine($"Error in SerialNumbers GetAll: {ex.Message}");
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var entity = await _context.SerialNumbers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null) return NotFound();

        return Ok(new SerialNumberDto
        {
            Id = entity.Id,
            Number = entity.Number,
            Status = entity.Status,
            ProductId = entity.ProductId,
            RfidTag = entity.RfidTag,
            DataMatrix = entity.DataMatrix
        });
    }



    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(SerialNumberDto dto)
    {
        var entity = new SerialNumber
        {
            Number = dto.Number,
            Status = dto.Status,
            ProductId = dto.ProductId,
            RfidTag = dto.RfidTag,
            DataMatrix = dto.DataMatrix
        };

        _context.SerialNumbers.Add(entity);
        await _context.SaveChangesAsync();

        dto.Id = entity.Id;
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, dto);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, SerialNumberDto dto)
    {
        var entity = await _context.SerialNumbers.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Number = dto.Number;
        entity.Status = dto.Status;
        entity.ProductId = dto.ProductId;
        entity.RfidTag = dto.RfidTag;
        entity.DataMatrix = dto.DataMatrix;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.SerialNumbers.FindAsync(id);
        if (entity == null) return NotFound();

        _context.SerialNumbers.Remove(entity);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}