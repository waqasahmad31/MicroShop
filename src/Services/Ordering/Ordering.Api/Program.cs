using System.Diagnostics;
using Ordering.Api;
using Ordering.Infrastructure;

var seedOnly = args.Contains("--seed");
var builder = WebApplication.CreateBuilder(args.Where(arg => arg != "--seed").ToArray());

builder.Services.AddOrderingInfrastructure(builder.Configuration);
builder.Services.AddExceptionHandler<OrderingExceptionHandler>();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
builder.Services.AddOpenApi();
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow);

var app = builder.Build();
if (seedOnly)
{
    if (!app.Environment.IsDevelopment()) throw new InvalidOperationException("Seeding is allowed only in Development.");
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<OrderingSeedData>().SeedAsync(CancellationToken.None);
    return;
}
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "MicroShop Ordering v1"));
}

app.MapGet("/", () => Results.Ok(new
{
    Service = "Ordering",
    Phase = "Ordering"
}));

app.MapOrderingEndpoints();
app.Run();

public partial class Program { }
