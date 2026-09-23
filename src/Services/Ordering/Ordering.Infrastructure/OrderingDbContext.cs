using Microsoft.EntityFrameworkCore;
using Ordering.Domain;

namespace Ordering.Infrastructure;

public sealed class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var order = modelBuilder.Entity<Order>();
        order.ToTable("orders", table =>
        {
            table.HasCheckConstraint("ck_orders_ids", "id <> '00000000-0000-0000-0000-000000000000'::uuid AND customer_id <> '00000000-0000-0000-0000-000000000000'::uuid");
            table.HasCheckConstraint("ck_orders_status", "status IN ('Pending', 'Confirmed', 'Rejected', 'Cancelled')");
            table.HasCheckConstraint("ck_orders_reason", "(status <> 'Rejected' OR (status_reason IS NOT NULL AND length(btrim(status_reason)) > 0)) AND (status NOT IN ('Pending', 'Confirmed') OR status_reason IS NULL)");
        });
        order.HasKey(x => x.Id).HasName("pk_orders");
        order.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        order.Property(x => x.CustomerId).HasColumnName("customer_id");
        order.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        order.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16);
        order.Property(x => x.StatusReason).HasColumnName("status_reason").HasMaxLength(500);
        order.Ignore(x => x.Total);
        order.Ignore(x => x.Currency);
        order.HasIndex(x => new { x.CustomerId, x.CreatedAtUtc, x.Id }).HasDatabaseName("ix_orders_customer_created_id");
        order.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_order_items_orders");
        order.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        var item = modelBuilder.Entity<OrderItem>();
        item.ToTable("order_items", table =>
        {
            table.HasCheckConstraint("ck_order_items_product", "product_id <> '00000000-0000-0000-0000-000000000000'::uuid");
            table.HasCheckConstraint("ck_order_items_quantity", "quantity BETWEEN 1 AND 1000");
            table.HasCheckConstraint("ck_order_items_price", "unit_price BETWEEN 0 AND 99999999.99");
            table.HasCheckConstraint("ck_order_items_name", "length(btrim(product_name)) > 0");
        });
        item.HasKey(x => new { x.OrderId, x.ProductId }).HasName("pk_order_items");
        item.Property(x => x.OrderId).HasColumnName("order_id").ValueGeneratedNever();
        item.Property(x => x.ProductId).HasColumnName("product_id").ValueGeneratedNever();
        item.Property(x => x.ProductName).HasColumnName("product_name").HasMaxLength(120).IsRequired();
        item.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(10, 2);
        item.Property(x => x.Quantity).HasColumnName("quantity");
        item.Ignore(x => x.LineTotal);
    }
}
