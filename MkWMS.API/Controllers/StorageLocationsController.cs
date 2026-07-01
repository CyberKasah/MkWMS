using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MkWMS.API.DTOs;
using MkWMS.Data.Context;
using MkWMS.Data.Entities;

namespace MkWMS.API.Controllers;

[ApiController]
[Route("api/storagelocations")]
[Authorize]
public class StorageLocationsController : ControllerBase
{
    private readonly MkWMSDbContext _context;

    public StorageLocationsController(MkWMSDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<StorageLocationDto>>> GetAll([FromQuery] PagedRequestDto req)
    {
        var query = _context.StorageLocations.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.ToLower().Trim();
            query = query.Where(x => x.Name.ToLower().Contains(s) || (x.RfidTag != null && x.RfidTag.Contains(s)));
        }

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(x => new StorageLocationDto
            {
                Id = x.Id,
                WarehouseId = x.WarehouseId,
                Name = x.Name,
                RfidTag = x.RfidTag,
                CellType = x.CellType
            }).ToListAsync();

        return Ok(new PagedResult<StorageLocationDto> { TotalCount = totalCount, Items = data });
    }
    [HttpPost]
    public async Task<IActionResult> Create(StorageLocationDto dto)
    {
        var entity = new StorageLocation
        {
            WarehouseId = dto.WarehouseId,
            Name = dto.Name,
            RfidTag = dto.RfidTag,
            CellType = dto.CellType
        };

        _context.StorageLocations.Add(entity);
        await _context.SaveChangesAsync();

        dto.Id = entity.Id;
        return CreatedAtAction(nameof(GetAll), new { id = entity.Id }, dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, StorageLocationDto dto)
    {
        var entity = await _context.StorageLocations.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Name = dto.Name;
        entity.RfidTag = dto.RfidTag;
        entity.CellType = dto.CellType;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.StorageLocations.FindAsync(id);
        if (entity == null) return NotFound();

        _context.StorageLocations.Remove(entity);
        await _context.SaveChangesAsync();
        return NoContent();
    }


    [HttpGet("by-rfid/{rfid}")]
    public async Task<IActionResult> GetByRfid(string rfid)
    {

        var serial = await _context.SerialNumbers.AsNoTracking().FirstOrDefaultAsync(s => s.RfidTag == rfid);
        if (serial != null)
            return Ok(new { Type = "SerialNumber", Id = serial.Id, Number = serial.Number, ProductId = serial.ProductId });


        var location = await _context.StorageLocations.AsNoTracking().FirstOrDefaultAsync(l => l.RfidTag == rfid);
        if (location != null)
            return Ok(new { Type = "StorageLocation", Id = location.Id, Name = location.Name });


        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.RfidBaseTag == rfid);
        if (product != null)
            return Ok(new { Type = "Product", Id = product.Id, Name = product.Name });

        return NotFound("Метка не зарегистрирована в системе");

    }
}