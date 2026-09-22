namespace Catalog.Application;

public sealed class CatalogNotFoundException(string resource, Guid id)
    : Exception($"{resource} '{id}' was not found.");

public sealed class CatalogConflictException(string message) : Exception(message);
