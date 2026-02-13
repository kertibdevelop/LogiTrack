using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class LogiTrackContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<InventoryItem> InventoryItems { get; set; }
    public DbSet<Order> Orders { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=logitrack.db");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure One-to-Many relationship: Order -> InventoryItem
        modelBuilder.Entity<InventoryItem>()
            .HasOne(i=>i.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(i => i.OrderId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }

    public static void Test()
    {
        using (var context = new LogiTrackContext())
        {
            // Ensure database and tables are created
            //context.Database.EnsureCreated();

            // Add test order item if none exist
            if (!context.Orders.Any())
            {
                var order = new Order{
                    CustomerName = "Acme Corp",
                    DatePlaced = DateTime.Now
                }.AddInventoryItem(new InventoryItem {
                    Name = "Pallet Jack",
                    Quantity = 12,
                    Location = "Warehouse A"
                });

                context.Orders.Add(order);
                context.SaveChanges();
            }

            // Retrieve and print inventory to confirm
            var orders = context.Orders
                .Include(o => o.Items)
                .ToList();

            foreach (var order in orders)
            {
                Console.WriteLine(order.GetOrderSummary());
            }
        }
    }

    
}