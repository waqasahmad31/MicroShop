using System.Diagnostics;
using Catalog.Api;
using Catalog.Infrastructure;

var seedOnly = args.Contains("--seed");
var builder = WebApplication.CreateBuilder(args.Where(arg => arg != "--seed").ToArray());

builder.Services.AddCatalogInfrastructure(builder.Configuration);
builder.Services.AddExceptionHandler<CatalogExceptionHandler>();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
builder.Services.AddOpenApi();
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

var app = builder.Build();
if (seedOnly)
{
    if (!app.Environment.IsDevelopment()) throw new InvalidOperationException("Seeding is allowed only in Development.");
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<CatalogSeedData>().SeedAsync(CancellationToken.None);
    return;
}
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "MicroShop Catalog v1"));
}

app.MapGet("/", () => Results.Ok(new
{
    Service = "Catalog",
    Phase = "Catalog"
}));

app.MapCatalogEndpoints();
app.Run();

public partial class Program { }
