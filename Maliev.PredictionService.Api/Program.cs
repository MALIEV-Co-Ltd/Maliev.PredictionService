using Maliev.Aspire.ServiceDefaults;
using Maliev.PredictionService.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// --- Infrastructure & Observability ---
builder.AddServiceDefaults(); // OpenTelemetry, health checks, resilience

// --- PredictionService Dependencies ---
builder.Services.AddPredictionService(builder.Configuration);

// --- API Infrastructure ---
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// --- Authentication & Authorization ---
// JWT Authentication with permission-based authorization (AddJwtAuthentication includes AddPermissionAuthorization)
builder.AddJwtAuthentication();

// --- API Configuration ---
builder.AddDefaultApiVersioning(); // API versioning with URL segment reader (FR-051)

// CORS (if needed)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// --- Middleware Pipeline ---

// Map health check and metrics endpoints via ServiceDefaults
app.MapDefaultEndpoints("predictionservice");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

app.Run();

// Make Program accessible to integration tests
public partial class Program { }
