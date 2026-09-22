var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();
app.UseExceptionHandler();

// Service routes arrive in Phase 7; the gateway never owns business use cases.
app.MapReverseProxy();
app.MapGet("/", () => Results.Ok(new { Service = "Gateway", Phase = "Solution skeleton" }));

app.Run();

