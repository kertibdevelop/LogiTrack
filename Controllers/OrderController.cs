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

    public OrderController(LogiTrackContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetAllOrders()
    {
        
        var orders = await _context.Orders
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

        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrderById(int id)
    {
        var order = await _context.Orders
            .Where(o => o.OrderId == id)
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

        return NoContent();
    }
}