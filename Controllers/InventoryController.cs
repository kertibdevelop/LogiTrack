using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
[Authorize]                  
public class InventoryController : ControllerBase
{
    private readonly LogiTrackContext _context;   
    private readonly IMemoryCache _cache;

    public InventoryController(LogiTrackContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }


    [HttpGet]
    public async Task<ActionResult<IEnumerable<InventoryItem>>> GetAllItems()
    {
        const string caheKey ="AllInventoryItems";

        if(_cache.TryGetValue(caheKey, out List<InventoryItem>? cachedItems) && cachedItems != null)
        {
            return Ok(cachedItems);
        }
        var items = await _context.InventoryItems
            .ToListAsync();                     

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };
        _cache.Set(caheKey, items, cacheOptions);
        return Ok(items);                       
    }

    [HttpPost]
    [Authorize(Roles = "Manager")]   
    public async Task<ActionResult<InventoryItem>> CreateItem([FromBody] InventoryItemCreateDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest("The Item cannot.");
        }

        var item=new InventoryItem
        {
            Name = dto.Name,
            Quantity = dto.Quantity,
            Location = dto.Location ?? "Unassigned",
            OrderId = dto.OrderId
        };

        _context.InventoryItems.Add(item);
        await _context.SaveChangesAsync();

        _cache.Remove("AllInventoryItems");

        return CreatedAtAction(
            nameof(GetAllItems),                
            new { id = item.ItemId },
            item
        );
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager")]   
    public async Task<IActionResult> DeleteItem(int id)
    {
        var item = await _context.InventoryItems
            .FindAsync(id);

        if (item == null)
        {
            return NotFound();                  
        }

        _context.InventoryItems.Remove(item);
        await _context.SaveChangesAsync();

        _cache.Remove("AllInventoryItems");

        return NoContent();                     
    }
}