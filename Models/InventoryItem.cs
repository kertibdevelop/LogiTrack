using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class InventoryItem
{
    [Key]
    public int ItemId { get; set; }
    public string Name { get; set; }
    public int Quantity { get; set; }
    public string Location { get; set; }

    // Foreign Key - Optional
    public int? OrderId { get; set; }

    // Navigation Property - Optional
    public Order? Order { get; set; }

    public void DisplayInfo()
    {
        Console.WriteLine(ToString());
    }

    public override string ToString()
    {
        return $"Item: {Name} | Quantity: {Quantity} | Location: {Location}";
    }
}