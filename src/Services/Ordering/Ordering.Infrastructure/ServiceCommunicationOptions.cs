namespace Ordering.Infrastructure;

public sealed class ServiceEndpointOptions
{
    public string BaseUrl { get; set; } = "";
    public int TimeoutMilliseconds { get; set; } = 3000;

    public bool IsValid() => Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri)
        && (uri.Scheme == "http" || uri.Scheme == "https") && !string.IsNullOrEmpty(uri.Host)
        && uri.AbsolutePath == "/" && uri.Query == "" && uri.Fragment == "" && uri.UserInfo == ""
        && TimeoutMilliseconds is >= 100 and <= 30000;
}

public sealed class ServiceCommunicationOptions
{
    public ServiceEndpointOptions Catalog { get; set; } = new();
    public ServiceEndpointOptions Inventory { get; set; } = new();
    public int CheckoutTimeoutMilliseconds { get; set; } = 15000;
}
