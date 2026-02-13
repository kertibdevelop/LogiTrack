using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
[Authorize]                    
public class OrderController : ControllerBase
{
    private readonly LogiTrackContext _context;
    private readonly IMemoryCache _cache;
    //const string allOrdersCacheKey = "AllOrders";


    public OrderController(LogiTrackContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetAllOrders()
    {
        if (_cache.TryGetValue(CacheKeys.AllOrders, out List<OrderDto>? cachedOrders) && cachedOrders != null)
        {
            return Ok(cachedOrders);
        }

        var orders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Select(o=> new OrderDto {
                OrderId = o.OrderId,
                CustomerName = o.CustomerName,
                DatePlaced = o.DatePlaced,
                Items = o.Items.Select(i => new InventoryItemDto {
                    ItemId = i.ItemId,
                    Name = i.Name,
                    Quantity = i.Quantity,
                    Location = i.Location,
                    OrderId = i.OrderId ?? 0
                }).ToList()
            })
            .ToListAsync();

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
        };

        _cache.Set(CacheKeys.AllOrders, orders, cacheOptions);

        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrderById(int id)
    {

        string singleOrderCacheKey = string.Format(CacheKeys.OrderById, id);

        if (_cache.TryGetValue(CacheKeys.AllOrders, out List<OrderDto>? cachedAllOrders) && cachedAllOrders != null)
        {
            var orderFromAll = cachedAllOrders.FirstOrDefault(o => o.OrderId == id);
            if (orderFromAll != null)
            {
                _cache.Set(singleOrderCacheKey, orderFromAll, TimeSpan.FromSeconds(30));
                return Ok(orderFromAll);
            }
        }

        if (_cache.TryGetValue(singleOrderCacheKey, out OrderDto? cachedSingleOrder) && cachedSingleOrder != null)
        {
            return Ok(cachedSingleOrder);
        }

        var order = await _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderId == id)
            .Include(o => o.Items)
            .Select(o => new OrderDto {
                OrderId = o.OrderId,
                CustomerName = o.CustomerName,
                DatePlaced = o.DatePlaced,
                Items = o.Items.Select(i => new InventoryItemDto {
                    ItemId = i.ItemId,
                    Name = i.Name,
                    Quantity = i.Quantity,
                    Location = i.Location,
                    OrderId = i.OrderId ?? 0
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (order == null)
        {
            return NotFound();
        }

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
        };

        _cache.Set(singleOrderCacheKey, order, cacheOptions);

        if (_cache.TryGetValue(CacheKeys.AllOrders, out List<OrderDto>? existingAll) && existingAll != null)
        {
            var updatedAll = existingAll.Where(o => o.OrderId != id).ToList();
            updatedAll.Add(order);
            _cache.Set(CacheKeys.AllOrders, updatedAll, cacheOptions);
        }

        return Ok(order);
    }

    [HttpPost]
    [Authorize(Roles = "Manager")]   
    public async Task<ActionResult<Order>> CreateOrder([FromBody] OrderCreateDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.CustomerName))
        {
            return BadRequest("Missing required fields: CustomerName is required.");
        }

        var order = new Order
        {
            CustomerName = dto.CustomerName,
            DatePlaced = DateTime.UtcNow,          
            Items = new List<InventoryItem>()
        };

        if (dto.ItemIds?.Any() == true)
        {
            var existingItems = await _context.InventoryItems
                .Where(i => dto.ItemIds.Contains(i.ItemId))
                .ToListAsync();

            foreach (var item in existingItems)
            {                
                order.Items.Add(item);
            }
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var responseDto = new OrderDto
        {
            OrderId = order.OrderId,
            CustomerName = order.CustomerName,
            DatePlaced = order.DatePlaced,
            Items = order.Items.Select(i => new InventoryItemDto
            {
                ItemId = i.ItemId,
                Name = i.Name,
                Quantity = i.Quantity,
                Location = i.Location,
                OrderId = i.OrderId ?? 0
            }).ToList()
        };

        _cache.Remove(CacheKeys.AllOrders);
        _cache.Remove(string.Format(CacheKeys.OrderById, order.OrderId));

        return CreatedAtAction(
            nameof(GetOrderById),
            new { id = order.OrderId },
            responseDto
        );
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager")]   
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)          
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order == null)
        {
            return NotFound();
        }

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();

        _cache.Remove(CacheKeys.AllOrders);
        _cache.Remove(string.Format(CacheKeys.OrderById, order.OrderId));

        return NoContent();
    }

    [HttpPost("{orderId}/items")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> AssignItemsToOrder(int orderId, [FromBody] AssignItemsToOrderDto dto)
    {
        if (dto?.ItemIds == null || !dto.ItemIds.Any())
            return BadRequest("At least one ItemId is required.");

        var order = await _context.Orders
            .Include(o => o.Items) 
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
            return NotFound("Order not found.");

        var itemsToAssign = await _context.InventoryItems
            .Where(i => dto.ItemIds.Contains(i.ItemId) && i.OrderId == null)
            .ToListAsync();

        if (itemsToAssign.Count != dto.ItemIds.Count)
        {
            var missing = dto.ItemIds.Except(itemsToAssign.Select(i => i.ItemId));
            return BadRequest($"Some items not found or already assigned: {string.Join(", ", missing)}");
        }

        foreach (var item in itemsToAssign)
        {
            item.OrderId = orderId;
            order.Items.Add(item);
            _cache.Remove(string.Format(CacheKeys.InventoryItemById, item.ItemId));
        }

        await _context.SaveChangesAsync();

        _cache.Remove(CacheKeys.AllOrders);
        _cache.Remove(string.Format(CacheKeys.OrderById, orderId));
        _cache.Remove(CacheKeys.AllInventoryItems);

        return NoContent(); 
    }
}