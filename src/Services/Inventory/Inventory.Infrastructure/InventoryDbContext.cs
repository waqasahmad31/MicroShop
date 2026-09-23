using Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<InventoryItem> Items => Set<InventoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var item = modelBuilder.Entity<InventoryItem>();
        item.ToTable("inventory_items", table =>
        {
            table.HasCheckConstraint("ck_inventory_on_hand", "on_hand >= 0");
            table.HasCheckConstraint("ck_inventory_reserved", "reserved >= 0 AND reserved <= on_hand");
            table.HasCheckConstraint("ck_inventory_product_id", "product_id <> '00000000-0000-0000-0000-000000000000'::uuid");
        });
        item.HasKey(x => x.ProductId).HasName("pk_inventory_items");
        item.Property(x => x.ProductId).HasColumnName("product_id").ValueGeneratedNever();
        item.Property(x => x.OnHand).HasColumnName("on_hand");
        item.Property(x => x.Reserved).HasColumnName("reserved");
        item.Ignore(x => x.Available);
        // ProductId is an external identifier, not a foreign key to another service's database.
    }
}
