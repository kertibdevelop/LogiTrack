using System.ComponentModel.DataAnnotations;

public class Order
{
    [Key]
    public int OrderId { get; set;}
    public string CustomerName { get; set; }
    public DateTime DatePlaced { get; set; }  

    // Navigation Property - One-to-Many relationship
    public ICollection<InventoryItem> Items { get; set; } = new List<InventoryItem>();

    public Order AddInventoryItem(InventoryItem item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));
        
        Items.Add(item);
        return this;
    }

    public Order RemoveInventoryItem(InventoryItem item)
    {
        if (item != null)
            Items.Remove(item);
        
        return this;
    }

    public string GetOrderSummary()
    {
        if (Items.Count == 0)
        {
            return $"Order {OrderId} for {CustomerName} has no items.";
        }

        var itemSummaries = Items.Select(i => i.ToString());
        return $"Order {OrderId} for {CustomerName} includes:\n" + string.Join("\n", itemSummaries);
    }
}