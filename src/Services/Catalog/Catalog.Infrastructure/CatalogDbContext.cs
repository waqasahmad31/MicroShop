using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var category = modelBuilder.Entity<Category>();
        category.ToTable("categories");
        category.HasKey(x => x.Id);
        category.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        category.Property(x => x.Name).HasColumnName("name").HasMaxLength(80).IsRequired();
        category.Property(x => x.NormalizedName).HasColumnName("normalized_name").HasMaxLength(160).IsRequired();
        category.Property(x => x.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
        category.HasIndex(x => x.NormalizedName).IsUnique().HasDatabaseName("ux_categories_normalized_name");

        var product = modelBuilder.Entity<Product>();
        product.ToTable("products", table => table.HasCheckConstraint("ck_products_price", "unit_price >= 0 AND unit_price <= 99999999.99"));
        product.HasKey(x => x.Id);
        product.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        product.Property(x => x.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        product.Property(x => x.Description).HasColumnName("description").HasMaxLength(2000).IsRequired();
        product.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(10, 2);
        product.Property(x => x.CategoryId).HasColumnName("category_id");
        // This FK is safe: both tables belong to Catalog. No other service database is referenced.
        product.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_products_categories");
        product.HasIndex(x => x.CategoryId).HasDatabaseName("ix_products_category_id");
    }
}
