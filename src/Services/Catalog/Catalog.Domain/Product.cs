namespace Catalog.Domain;

public sealed class Product
{
    public const decimal MaximumPrice = 99_999_999.99m;
    private Product() { }

    public Product(Guid id, string? name, string? description, decimal unitPrice, Guid categoryId)
    {
        if (id == Guid.Empty) throw new ArgumentException("An ID is required.", nameof(id));
        Id = id;
        Update(name, description, unitPrice, categoryId);
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public string Description { get; private set; } = "";
    public decimal UnitPrice { get; private set; }
    public Guid CategoryId { get; private set; }

    public void Update(string? name, string? description, decimal unitPrice, Guid categoryId)
    {
        var validName = CatalogRules.Name(name, 120);
        var validDescription = CatalogRules.Description(description, 2000);
        if (unitPrice < 0 || unitPrice > MaximumPrice || decimal.Round(unitPrice, 2) != unitPrice)
            throw new CatalogValidationException("UnitPrice", $"Price must be between 0 and {MaximumPrice} with at most two decimal places.");
        if (categoryId == Guid.Empty)
            throw new CatalogValidationException("CategoryId", "A category ID is required.");

        Name = validName;
        Description = validDescription;
        UnitPrice = unitPrice;
        CategoryId = categoryId;
    }
}
