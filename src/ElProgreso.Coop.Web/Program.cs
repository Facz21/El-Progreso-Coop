using ElProgreso.Coop.Application.DTOs;
using ElProgreso.Coop.Application.Interfaces;
using ElProgreso.Coop.Application.Services;
using ElProgreso.Coop.Domain.Enums;
using ElProgreso.Coop.Infrastructure.Data;
using ElProgreso.Coop.Infrastructure.Repositories;
using ElProgreso.Coop.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Determine database path
var configuredDbPath = builder.Configuration.GetValue<string>("Database:Path") ?? "elprogreso.db";
var dbPath = configuredDbPath;
if (!File.Exists(dbPath))
{
    // Try parent directory if running from src/ElProgreso.Coop.Web
    var parentDbPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "elprogreso.db");
    if (File.Exists(parentDbPath))
    {
        dbPath = Path.GetFullPath(parentDbPath);
    }
}

// Infrastructure DI
builder.Services.AddSingleton<LiteDbContext>(_ => new LiteDbContext(dbPath));
builder.Services.AddScoped<IAssociateRepository, LiteDbAssociateRepository>();
builder.Services.AddScoped<ITransactionRepository, LiteDbTransactionRepository>();
builder.Services.AddHttpClient<IExchangeRateService, ExchangeRateService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
});

// Application DI
builder.Services.AddScoped<IBankingService, BankingService>();
builder.Services.AddScoped<IManagementReportService, ManagementReportService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Auto-seed if database is empty
using (var scope = app.Services.CreateScope())
{
    var assocRepo = scope.ServiceProvider.GetRequiredService<IAssociateRepository>();
    var txRepo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
    await DatabaseSeeder.SeedIfEmptyAsync(assocRepo, txRepo);
}

app.UseCors();

// Serve frontend static files (from wwwroot: index.html, css/, js/)
app.UseDefaultFiles();
app.UseStaticFiles();

// --- API ENDPOINTS ---

// Associates API
var associatesApi = app.MapGroup("/api/associates");

associatesApi.MapGet("/", async (string? query, IBankingService bankingService) =>
{
    if (!string.IsNullOrWhiteSpace(query))
    {
        var searchResults = await bankingService.SearchAssociatesAsync(query);
        return Results.Ok(searchResults);
    }
    var all = await bankingService.GetAllAssociatesAsync();
    return Results.Ok(all);
});

associatesApi.MapGet("/{document}", async (string document, IBankingService bankingService) =>
{
    var associate = await bankingService.GetAssociateByDocumentAsync(document);
    return associate is not null ? Results.Ok(associate) : Results.NotFound(new { message = $"Asociado con documento {document} no encontrado." });
});

associatesApi.MapPost("/", async (RegisterAssociateRequest req, IBankingService bankingService) =>
{
    try
    {
        var created = await bankingService.RegisterAssociateAsync(
            req.Document,
            req.Name,
            req.DocumentType,
            req.Phone,
            req.Email,
            req.Address);
        return Results.Created($"/api/associates/{created.Document}", created);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

associatesApi.MapPut("/{document}", async (string document, UpdateAssociateProfileRequest req, IBankingService bankingService) =>
{
    try
    {
        var updated = await bankingService.UpdateAssociateProfileAsync(document, req.Name, req.Phone, req.Email, req.Address);
        return Results.Ok(updated);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

associatesApi.MapDelete("/{document}", async (string document, IBankingService bankingService) =>
{
    try
    {
        await bankingService.DeleteAssociateAsync(document);
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// Transactions API
var transactionsApi = app.MapGroup("/api/transactions");

transactionsApi.MapGet("/associate/{document}", async (string document, IBankingService bankingService) =>
{
    var transactions = await bankingService.GetAssociateTransactionsAsync(document);
    return Results.Ok(transactions);
});

transactionsApi.MapPost("/deposit", async (TransactionRequest req, IBankingService bankingService) =>
{
    try
    {
        var tx = await bankingService.DepositAsync(req.Document, req.Amount);
        return Results.Ok(tx);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

transactionsApi.MapPost("/withdraw", async (TransactionRequest req, IBankingService bankingService) =>
{
    try
    {
        var tx = await bankingService.WithdrawAsync(req.Document, req.Amount);
        return Results.Ok(tx);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// TRM Currency API
app.MapGet("/api/trm/live", async (IExchangeRateService exchangeRateService) =>
{
    var rate = await exchangeRateService.GetUsdExchangeRateAsync();
    return Results.Ok(rate);
});

// Management Reports API
var reportsApi = app.MapGroup("/api/reports");

reportsApi.MapGet("/overview", async (IManagementReportService reportService) =>
{
    var report = await reportService.GetCooperativeOverviewAsync();
    return Results.Ok(report);
});

reportsApi.MapGet("/top-associates", async (IManagementReportService reportService) =>
{
    var report = await reportService.GetTop5AssociatesByBalanceAsync();
    return Results.Ok(report);
});

reportsApi.MapGet("/dormant-associates", async (IManagementReportService reportService) =>
{
    var report = await reportService.GetDormantAssociatesAsync();
    return Results.Ok(report);
});

reportsApi.MapGet("/largest-transactions", async (IManagementReportService reportService) =>
{
    var report = await reportService.GetTop10LargestTransactionsAsync();
    return Results.Ok(report);
});

reportsApi.MapGet("/cashier-movement", async (IManagementReportService reportService) =>
{
    var report = await reportService.GetCashierMovementSummaryPerAssociateAsync();
    return Results.Ok(report);
});

reportsApi.MapGet("/date-range-summary", async (DateTime? start, DateTime? end, IManagementReportService reportService) =>
{
    var s = start ?? DateTime.UtcNow.AddMonths(-1);
    var e = end ?? DateTime.UtcNow;
    var report = await reportService.GetDateRangeSummaryAsync(s, e);
    return Results.Ok(report);
});

app.Run();

// Request DTO records
public record RegisterAssociateRequest(string Document, string Name, DocumentType DocumentType = DocumentType.CC, string? Phone = null, string? Email = null, string? Address = null);
public record UpdateAssociateProfileRequest(string Name, string? Phone = null, string? Email = null, string? Address = null);
public record TransactionRequest(string Document, decimal Amount);
