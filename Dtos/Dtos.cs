using System.ComponentModel.DataAnnotations;

public class OrderCreateDto
{
    public string CustomerName { get; set; } = string.Empty;
    public List<int>? ItemIds { get; set; }
}

public class InventoryItemIncludeDto
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Location { get; set; }
}

public class InventoryItemCreateDto
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Location { get; set; }
    public int? OrderId { get; set; }
}

public class OrderDto
{
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime DatePlaced { get; set; }

    public List<InventoryItemDto> Items { get; set; } = new();
}

public class InventoryItemDto
{
    public int ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Location { get; set; } = string.Empty;
    public int OrderId { get; set; }

   
}

public class RegisterDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? UserName { get; set; }     // opcionális
}

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}