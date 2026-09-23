using System.Diagnostics;
using Inventory.Api;
using Inventory.Infrastructure;

var seedOnly = args.Contains("--seed");
var builder = WebApplication.CreateBuilder(args.Where(arg => arg != "--seed").ToArray());

builder.Services.AddInventoryInfrastructure(builder.Configuration);
builder.Services.AddExceptionHandler<InventoryExceptionHandler>();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
builder.Services.AddOpenApi();
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

var app = builder.Build();
if (seedOnly)
{
    if (!app.Environment.IsDevelopment()) throw new InvalidOperationException("Seeding is allowed only in Development.");
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<InventorySeedData>().SeedAsync(CancellationToken.None);
    return;
}
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "MicroShop Inventory v1"));
}

app.MapGet("/", () => Results.Ok(new
{
    Service = "Inventory",
    Phase = "Inventory"
}));

app.MapInventoryEndpoints();
app.Run();

public partial class Program { }
