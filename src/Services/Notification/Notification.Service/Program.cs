var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
app.MapGet("/", () => Results.Ok(new { Service = "Notification", Phase = "Solution skeleton" }));

app.Run();

