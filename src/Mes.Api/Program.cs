using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Logging estruturado ───────────────────────────────────────────────
// Serilog lê a configuração de appsettings.json (seção "Serilog").
// Log estruturado significa propriedades nomeadas e pesquisáveis
// (workOrderId, resourceId, traceId) — ver requirements.md 12.1.
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// ── OpenAPI ───────────────────────────────────────────────────────────
// Gera o documento em /openapi/v1.json. A UI do Scalar consome esse documento.
builder.Services.AddOpenApi();

// ── Health checks ─────────────────────────────────────────────────────
// Neste sprint só o self-check. No Sprint 3, quando o DbContext existir,
// adicionamos a verificação do PostgreSQL aqui.
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

var app = builder.Build();

// ── Pipeline ──────────────────────────────────────────────────────────
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();   // UI navegável em /scalar
}

// GET /health — público, sem autenticação (requirements.md 8.4, 10.6).
// Responde 200 quando todos os checks registrados estão saudáveis, 503 quando não.
app.MapHealthChecks("/health");

app.Run();