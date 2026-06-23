using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MkWMS.API.DTOs;
using MkWMS.Data.Context;
using MkWMS.Data.Entities;

namespace MkWMS.API.Controllers;

[ApiController]
[Route("api/serialnumbers")]
[Authorize] // Чтение доступно всем авторизованным пользователям
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
            // 1. Валидация параметров
            var page = req.Page < 1 ? 1 : req.Page;
            var pageSize = req.PageSize < 1 ? 20 : req.PageSize;

            var query = _context.SerialNumbers.AsNoTracking().AsQueryable();

            // 2. Поиск (исправленный)
            if (!string.IsNullOrWhiteSpace(req.Search))
            {
                var s = req.Search.ToLower().Trim();
                query = query.Where(x => x.Number.ToLower().Contains(s));
            }

            // 3. Считаем общее кол-во
            var totalCount = await query.CountAsync();

            // 4. Получаем данные
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
            RfidTag = x.RfidTag // ТЕПЕРЬ ПРИВЯЗКА В WPF ЗАРАБОТАЕТ
        }).ToListAsync();

            return Ok(new PagedResult<SerialNumberDto>
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Items = data ?? new List<SerialNumberDto>() // Гарантируем, что Items не null
            });
        }
        catch (Exception ex)
        {
            // Если упадет — увидишь ошибку в консоли API
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
            ProductId = entity.ProductId
        });
    }

    [HttpPost]
    [Authorize(Policy = "AdminPolicy")] // Создание только для админов
    public async Task<IActionResult> Create(SerialNumberDto dto)
    {
        var entity = new SerialNumber
        {
            Number = dto.Number,
            Status = dto.Status,
            ProductId = dto.ProductId
        };

        _context.SerialNumbers.Add(entity);
        await _context.SaveChangesAsync();

        dto.Id = entity.Id;
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, dto);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminPolicy")] // Обновление только для админов
    public async Task<IActionResult> Update(int id, SerialNumberDto dto)
    {
        var entity = await _context.SerialNumbers.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Number = dto.Number;
        entity.Status = dto.Status;
        entity.ProductId = dto.ProductId;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminPolicy")] // Удаление только для админов
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.SerialNumbers.FindAsync(id);
        if (entity == null) return NotFound();

        _context.SerialNumbers.Remove(entity);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}