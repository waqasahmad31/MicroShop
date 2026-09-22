namespace Catalog.Domain;

public sealed class Category
{
    private Category() { }

    public Category(Guid id, string? name, string? description)
    {
        if (id == Guid.Empty) throw new ArgumentException("An ID is required.", nameof(id));
        Id = id;
        Update(name, description);
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public string NormalizedName { get; private set; } = "";
    public string Description { get; private set; } = "";

    public void Update(string? name, string? description)
    {
        var validName = CatalogRules.Name(name, 80);
        var validDescription = CatalogRules.Description(description, 500);
        Name = validName;
        NormalizedName = validName.ToUpperInvariant();
        Description = validDescription;
    }
}
