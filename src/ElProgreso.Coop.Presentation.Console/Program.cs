using Microsoft.Extensions.DependencyInjection;
using ElProgreso.Coop.Application.Interfaces;
using ElProgreso.Coop.Application.Services;
using ElProgreso.Coop.Infrastructure.Data;
using ElProgreso.Coop.Infrastructure.Repositories;
using ElProgreso.Coop.Infrastructure.Services;
using ElProgreso.Coop.Presentation.Console;

var services = new ServiceCollection();

// Infrastructure
services.AddSingleton<LiteDbContext>(_ => new LiteDbContext("elprogreso.db"));
services.AddScoped<IAssociateRepository, LiteDbAssociateRepository>();
services.AddScoped<ITransactionRepository, LiteDbTransactionRepository>();
services.AddHttpClient<IExchangeRateService, ExchangeRateService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
});

// Application
services.AddScoped<IBankingService, BankingService>();
services.AddScoped<IManagementReportService, ManagementReportService>();

// Presentation
services.AddTransient<CashierApp>();

var serviceProvider = services.BuildServiceProvider();

// Auto-seed database if empty
using (var scope = serviceProvider.CreateScope())
{
    var assocRepo = scope.ServiceProvider.GetRequiredService<IAssociateRepository>();
    var txRepo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
    await DatabaseSeeder.SeedIfEmptyAsync(assocRepo, txRepo);
}

// Run app
var app = serviceProvider.GetRequiredService<CashierApp>();
await app.RunAsync();
