namespace Catalog.Domain;

internal static class CatalogRules
{
    public static string Name(string? value, int maxLength)
    {
        var text = value?.Trim() ?? "";
        if (text.Length == 0 || text.Length > maxLength)
            throw new CatalogValidationException("Name", $"Name must contain 1 to {maxLength} characters.");
        return text;
    }

    public static string Description(string? value, int maxLength)
    {
        var text = value?.Trim() ?? "";
        if (text.Length > maxLength)
            throw new CatalogValidationException("Description", $"Description cannot exceed {maxLength} characters.");
        return text;
    }
}
