using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MkWMS.API.DTOs;
using MkWMS.Data.Context;
using MkWMS.Data.Entities;

namespace MkWMS.API.Controllers;

[ApiController]
[Route("api/counterparties")]
[Authorize]
public class CounterpartiesController : ControllerBase
{
    private readonly MkWMSDbContext _context;

    public CounterpartiesController(MkWMSDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CounterpartyDto>>> GetAll([FromQuery] PagedRequestDto req)
    {
        var query = _context.Counterparties.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.ToLower().Trim();
            query = query.Where(x => x.Name.ToLower().Contains(s) || x.INN!.Contains(s));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.Name)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(x => new CounterpartyDto
            {
                Id = x.Id,
                Name = x.Name,
                INN = x.INN,
                KPP = x.KPP,
                Address = x.Address,
                IsSupplier = x.IsSupplier,
                IsCustomer = x.IsCustomer
            })
            .ToListAsync();

        return new PagedResult<CounterpartyDto> { Items = items, TotalCount = total };
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CounterpartyDto>> GetById(int id)
    {
        var x = await _context.Counterparties.FindAsync(id);
        if (x == null) return NotFound();

        return new CounterpartyDto
        {
            Id = x.Id,
            Name = x.Name,
            INN = x.INN,
            KPP = x.KPP,
            Address = x.Address,
            IsSupplier = x.IsSupplier,
            IsCustomer = x.IsCustomer
        };
    }

    [HttpPost]
    public async Task<ActionResult<CounterpartyDto>> Create(CounterpartyDto dto)
    {
        var entity = new Counterparty
        {
            Name = dto.Name,
            INN = dto.INN,
            KPP = dto.KPP,
            Address = dto.Address,
            IsSupplier = dto.IsSupplier,
            IsCustomer = dto.IsCustomer
        };

        _context.Counterparties.Add(entity);
        await _context.SaveChangesAsync();

        dto.Id = entity.Id;
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, CounterpartyDto dto)
    {
        var entity = await _context.Counterparties.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Name = dto.Name;
        entity.INN = dto.INN;
        entity.KPP = dto.KPP;
        entity.Address = dto.Address;
        entity.IsSupplier = dto.IsSupplier;
        entity.IsCustomer = dto.IsCustomer;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.Counterparties.FindAsync(id);
        if (entity == null) return NotFound();


        var hasDocs = await _context.Documents.AnyAsync(d => d.CounterpartyId == id);
        if (hasDocs) return BadRequest("Нельзя удалить контрагента, по которому есть документы");

        _context.Counterparties.Remove(entity);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}