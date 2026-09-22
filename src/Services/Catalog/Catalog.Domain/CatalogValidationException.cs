namespace Catalog.Domain;

public sealed class CatalogValidationException(string field, string message) : Exception(message)
{
    public string Field { get; } = field;
}
