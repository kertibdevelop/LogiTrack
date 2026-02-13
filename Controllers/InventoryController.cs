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
        if(_cache.TryGetValue(CacheKeys.AllInventoryItems, out List<InventoryItem>? cachedItems) && cachedItems != null)
        {
            return Ok(cachedItems);
        }
        var items = await _context.InventoryItems
            .ToListAsync();                     

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };
        _cache.Set(CacheKeys.AllInventoryItems, items, cacheOptions);
        return Ok(items);                       
    }

    [HttpGet("{id}")]
public async Task<ActionResult<InventoryItem>> GetInventoryItemById(int id)
{
    string singleItemCacheKey = string.Format(CacheKeys.InventoryItemById, id);

    if (_cache.TryGetValue(CacheKeys.AllInventoryItems, out List<InventoryItem>? cachedAllItems) && 
        cachedAllItems != null)
    {
        var itemFromAll = cachedAllItems.FirstOrDefault(i => i.ItemId == id);
        if (itemFromAll != null)
        {
            _cache.Set(singleItemCacheKey, itemFromAll, TimeSpan.FromSeconds(30));
            return Ok(itemFromAll);
        }
    }

    if (_cache.TryGetValue(singleItemCacheKey, out InventoryItem? cachedSingleItem) && 
        cachedSingleItem != null)
    {
        return Ok(cachedSingleItem);
    }

    var item = await _context.InventoryItems
        .AsNoTracking()
        .FirstOrDefaultAsync(i => i.ItemId == id);

    if (item == null)
    {
        return NotFound();
    }

    var cacheOptions = new MemoryCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
    };

    _cache.Set(singleItemCacheKey, item, cacheOptions);

    if (_cache.TryGetValue(CacheKeys.AllInventoryItems, out List<InventoryItem>? existingAll) && existingAll != null)
    {
        var updatedAll = existingAll.Where(i => i.ItemId != id).ToList();
        updatedAll.Add(item);
        _cache.Set(CacheKeys.AllInventoryItems, updatedAll, cacheOptions);
    }

    return Ok(item);
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

        _cache.Remove(CacheKeys.AllInventoryItems);
        _cache.Remove(string.Format(CacheKeys.InventoryItemById, item.ItemId));

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

        _cache.Remove(CacheKeys.AllInventoryItems);
        _cache.Remove(string.Format(CacheKeys.InventoryItemById, id));

        return NoContent();                     
    }
}