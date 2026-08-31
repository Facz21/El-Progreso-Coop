using ElProgreso.Coop.Domain.Entities;
using ElProgreso.Coop.Domain.Enums;
using ElProgreso.Coop.Infrastructure.Data;
using ElProgreso.Coop.Infrastructure.Repositories;
using ElProgreso.Coop.Infrastructure.Services;
using Xunit;

namespace ElProgreso.Coop.Tests;

public class InfrastructureTests : IDisposable
{
    private readonly string _dbPath;
    private readonly LiteDbContext _context;
    private readonly LiteDbAssociateRepository _associateRepo;
    private readonly LiteDbTransactionRepository _transactionRepo;

    public InfrastructureTests()
    {
        _dbPath = $"test_{Guid.NewGuid():N}.db";
        _context = new LiteDbContext(_dbPath);
        _associateRepo = new LiteDbAssociateRepository(_context);
        _transactionRepo = new LiteDbTransactionRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Fact]
    public async Task LiteDbRepositories_AddAssociateAndTransactions_PersistsCorrectly()
    {
        var associate = new Associate("1020304050", "Lucia Fernandez Gomez", DocumentType.CC, DateTime.UtcNow);
        await _associateRepo.AddAsync(associate);

        var deposit = associate.CreateDeposit(1_500_000m);
        await _transactionRepo.AddAsync(deposit);

        var withdrawal = associate.CreateWithdrawal(1_200_000m);
        await _transactionRepo.AddAsync(withdrawal);

        var retrieved = await _associateRepo.GetByDocumentAsync("1020304050");
        Assert.NotNull(retrieved);
        Assert.Equal("Lucia Fernandez Gomez", retrieved.Name);
        Assert.Equal(DocumentType.CC, retrieved.DocumentType);
        Assert.Equal(2, retrieved.Transactions.Count);
        // Deposit: 1.5M, Withdrawal: 1.2M + 8k fee => Balance: 292,000
        Assert.Equal(292_000m, retrieved.Balance);

        var hasTxs = await _transactionRepo.HasTransactionsAsync("1020304050");
        Assert.True(hasTxs);

        var searchResults = (await _associateRepo.SearchByNameAsync("lucia")).ToList();
        Assert.Single(searchResults);
        Assert.Equal("1020304050", searchResults[0].Document);
    }

    [Fact]
    public async Task ExchangeRateService_LiveOrFallback_ReturnsValidStructure()
    {
        var service = new ExchangeRateService();
        var result = await service.GetUsdExchangeRateAsync();

        if (result.IsSuccess)
        {
            Assert.True(result.Rate > 0m);
            Assert.NotNull(result.ValidFrom);
        }
        else
        {
            Assert.NotNull(result.ErrorMessage);
        }
    }
}
