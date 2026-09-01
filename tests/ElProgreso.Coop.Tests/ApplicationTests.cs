using ElProgreso.Coop.Application.Interfaces;
using ElProgreso.Coop.Application.Services;
using ElProgreso.Coop.Domain.Entities;
using ElProgreso.Coop.Domain.Enums;
using ElProgreso.Coop.Domain.Exceptions;
using Xunit;

namespace ElProgreso.Coop.Tests;

public class InMemoryAssociateRepository : IAssociateRepository
{
    public readonly Dictionary<string, Associate> Associates = new();

    public Task<Associate?> GetByDocumentAsync(string document)
    {
        Associates.TryGetValue(document, out var associate);
        return Task.FromResult(associate);
    }

    public Task<IEnumerable<Associate>> SearchByNameAsync(string namePattern)
    {
        var result = Associates.Values
            .Where(a => a.Name.Contains(namePattern, StringComparison.OrdinalIgnoreCase))
            .AsEnumerable();
        return Task.FromResult(result);
    }

    public Task<IEnumerable<Associate>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<Associate>>(Associates.Values.ToList());
    }

    public Task AddAsync(Associate associate)
    {
        Associates[associate.Document] = associate;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Associate associate)
    {
        Associates[associate.Document] = associate;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string document)
    {
        Associates.Remove(document);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string document)
    {
        return Task.FromResult(Associates.ContainsKey(document));
    }
}

public class InMemoryTransactionRepository : ITransactionRepository
{
    public readonly List<Transaction> Transactions = new();

    public Task<Transaction?> GetByIdAsync(Guid id)
    {
        var tx = Transactions.FirstOrDefault(t => t.Id == id);
        return Task.FromResult(tx);
    }

    public Task<IEnumerable<Transaction>> GetByAssociateDocumentAsync(string associateDocument)
    {
        var result = Transactions.Where(t => t.AssociateDocument == associateDocument).AsEnumerable();
        return Task.FromResult(result);
    }

    public Task<IEnumerable<Transaction>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<Transaction>>(Transactions.ToList());
    }

    public Task AddAsync(Transaction transaction)
    {
        Transactions.Add(transaction);
        return Task.CompletedTask;
    }

    public Task<bool> HasTransactionsAsync(string associateDocument)
    {
        return Task.FromResult(Transactions.Any(t => t.AssociateDocument == associateDocument));
    }
}

public class ApplicationTests
{
    [Fact]
    public async Task DeleteAssociate_WithNoTransactions_ShouldSucceed()
    {
        var assocRepo = new InMemoryAssociateRepository();
        var txRepo = new InMemoryTransactionRepository();
        var bankingService = new BankingService(assocRepo, txRepo);

        await bankingService.RegisterAssociateAsync("1020304050", "Ana Maria Gomez Perez");
        Assert.True(await assocRepo.ExistsAsync("1020304050"));

        await bankingService.DeleteAssociateAsync("1020304050");
        Assert.False(await assocRepo.ExistsAsync("1020304050"));
    }

    [Fact]
    public async Task DeleteAssociate_WithTransactions_ShouldThrowAssociateHasTransactionsException()
    {
        var assocRepo = new InMemoryAssociateRepository();
        var txRepo = new InMemoryTransactionRepository();
        var bankingService = new BankingService(assocRepo, txRepo);

        await bankingService.RegisterAssociateAsync("1020304050", "Ana Maria Gomez Perez");
        await bankingService.DepositAsync("1020304050", 100_000m);

        await Assert.ThrowsAsync<AssociateHasTransactionsException>(() => bankingService.DeleteAssociateAsync("1020304050"));
    }

    [Fact]
    public async Task SearchByName_CaseInsensitive_ShouldReturnMatches()
    {
        var assocRepo = new InMemoryAssociateRepository();
        var txRepo = new InMemoryTransactionRepository();
        var bankingService = new BankingService(assocRepo, txRepo);

        await bankingService.RegisterAssociateAsync("1020304051", "Carlos Alberto Mendoza Perez");
        await bankingService.RegisterAssociateAsync("1020304052", "Maria Camila Rodriguez Ortiz");
        await bankingService.RegisterAssociateAsync("1020304053", "Juan Carlos Gomez Lopez");

        var results = (await bankingService.SearchAssociatesAsync("carlos")).ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, a => a.Document == "1020304051");
        Assert.Contains(results, a => a.Document == "1020304053");
    }

    [Fact]
    public async Task ManagementReports_All6Reports_CalculatedAccurately()
    {
        var assocRepo = new InMemoryAssociateRepository();
        var txRepo = new InMemoryTransactionRepository();
        var bankingService = new BankingService(assocRepo, txRepo);
        var reportService = new ManagementReportService(assocRepo, txRepo);

        // Associate 1: 2 deposits (500k, 1.5M), 1 withdrawal (1.2M -> 8k commission) => Balance: 2M - 1.208M = 792,000
        var a1 = await bankingService.RegisterAssociateAsync("1020304001", "Associate One Perez Diaz");
        var t1 = await bankingService.DepositAsync("1020304001", 500_000m);
        var t2 = await bankingService.DepositAsync("1020304001", 1_500_000m);
        var t3 = await bankingService.WithdrawAsync("1020304001", 1_200_000m);

        // Associate 2: 1 deposit (300k) => Balance: 300,000
        var a2 = await bankingService.RegisterAssociateAsync("1020304002", "Associate Two Gomez Ruiz");
        var t4 = await bankingService.DepositAsync("1020304002", 300_000m);

        // Associate 3: Dormant (0 transactions) => Balance: 0
        var a3 = await bankingService.RegisterAssociateAsync("1020304003", "Associate Three Torres Mora");

        // 1. Cooperative Overview
        var overview = await reportService.GetCooperativeOverviewAsync();
        Assert.Equal(3, overview.TotalAssociates);
        Assert.Equal(792_000m + 300_000m, overview.TotalCooperativeBalance);
        Assert.Equal((792_000m + 300_000m) / 3, overview.AverageBalance);

        // 2. Top 5 Associates
        var top = (await reportService.GetTop5AssociatesByBalanceAsync()).ToList();
        Assert.Equal("1020304001", top[0].Document);
        Assert.Equal(792_000m, top[0].Balance);
        Assert.Equal("1020304002", top[1].Document);
        Assert.Equal(300_000m, top[1].Balance);

        // 3. Dormant Associates
        var dormant = (await reportService.GetDormantAssociatesAsync()).ToList();
        Assert.Single(dormant);
        Assert.Equal("1020304003", dormant[0].Document);

        // 4. Date Range Summary
        var rangeSummary = await reportService.GetDateRangeSummaryAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));
        Assert.Equal(4, rangeSummary.TotalTransactions);
        Assert.Equal(3, rangeSummary.DepositCount);
        Assert.Equal(1, rangeSummary.WithdrawalCount);
        Assert.Equal(2_300_000m, rangeSummary.TotalDeposited);
        Assert.Equal(1_200_000m, rangeSummary.TotalWithdrawn);
        Assert.Equal(8_000m, rangeSummary.TotalCommissions);
        Assert.Equal(2_300_000m - 1_208_000m, rangeSummary.NetDifference);

        // 5. Top 10 Largest Transactions
        var largest = (await reportService.GetTop10LargestTransactionsAsync()).ToList();
        Assert.Equal(4, largest.Count);
        Assert.Equal(1_500_000m, largest[0].Amount);
        Assert.Equal(1_200_000m, largest[1].Amount);
        Assert.Equal(500_000m, largest[2].Amount);
        Assert.Equal(300_000m, largest[3].Amount);

        // 6. Cashier Movement Summary
        var movements = (await reportService.GetCashierMovementSummaryPerAssociateAsync()).ToList();
        Assert.Equal(3, movements.Count);
        Assert.Equal("1020304001", movements[0].AssociateDocument);
        Assert.Equal(3, movements[0].TransactionCount);
        Assert.Equal(2_000_000m, movements[0].TotalDeposited);
        Assert.Equal(1_200_000m, movements[0].TotalWithdrawn);
        Assert.Equal(8_000m, movements[0].TotalCommissions);
    }

    [Fact]
    public async Task GetFilteredAssociates_WithDocumentTypeFilter_ReturnsMatchingOnly()
    {
        var assocRepo = new InMemoryAssociateRepository();
        var txRepo = new InMemoryTransactionRepository();
        var bankingService = new BankingService(assocRepo, txRepo);

        await bankingService.RegisterAssociateAsync("1020304051", "Carlos Alberto Mendoza Perez", DocumentType.CC);
        await bankingService.RegisterAssociateAsync("1098765432", "Juan David Lopez Gomez", DocumentType.TI);
        await bankingService.RegisterAssociateAsync("E1234567", "Jean Pierre Dubois Dupont", DocumentType.CE);

        var ccOnly = (await bankingService.GetFilteredAssociatesAsync(new ElProgreso.Coop.Application.DTOs.AssociateFilterCriteria(
            DocumentType: DocumentType.CC))).ToList();

        Assert.Single(ccOnly);
        Assert.Equal("1020304051", ccOnly[0].Document);
    }

    [Fact]
    public async Task GetFilteredAssociates_WithSorting_OrdersProperly()
    {
        var assocRepo = new InMemoryAssociateRepository();
        var txRepo = new InMemoryTransactionRepository();
        var bankingService = new BankingService(assocRepo, txRepo);

        var a1 = await bankingService.RegisterAssociateAsync("1020304001", "Bernardo Alberto Gomez Diaz");
        var a2 = await bankingService.RegisterAssociateAsync("1020304002", "Ana Maria Perez Lopez");
        var a3 = await bankingService.RegisterAssociateAsync("1020304003", "Carlos Eduardo Ruiz Romero");

        await bankingService.DepositAsync("1020304001", 100_000m);
        await bankingService.DepositAsync("1020304002", 500_000m);

        // Sort by Balance Descending
        var byBalanceDesc = (await bankingService.GetFilteredAssociatesAsync(new ElProgreso.Coop.Application.DTOs.AssociateFilterCriteria(
            SortBy: ElProgreso.Coop.Application.DTOs.AssociateSortField.BalanceDesc))).ToList();

        Assert.Equal("1020304002", byBalanceDesc[0].Document); // 500k
        Assert.Equal("1020304001", byBalanceDesc[1].Document); // 100k
        Assert.Equal("1020304003", byBalanceDesc[2].Document); // 0k

        // Sort by Name Ascending
        var byNameAsc = (await bankingService.GetFilteredAssociatesAsync(new ElProgreso.Coop.Application.DTOs.AssociateFilterCriteria(
            SortBy: ElProgreso.Coop.Application.DTOs.AssociateSortField.NameAsc))).ToList();

        Assert.Equal("Ana Maria Perez Lopez", byNameAsc[0].Name);
        Assert.Equal("Bernardo Alberto Gomez Diaz", byNameAsc[1].Name);
        Assert.Equal("Carlos Eduardo Ruiz Romero", byNameAsc[2].Name);
    }

    [Fact]
    public async Task RegisterAndModifyContactInfo_ShouldUpdateFieldsSuccessfully()
    {
        var assocRepo = new InMemoryAssociateRepository();
        var txRepo = new InMemoryTransactionRepository();
        var bankingService = new BankingService(assocRepo, txRepo);

        var a = await bankingService.RegisterAssociateAsync(
            "1020304050",
            "Carlos Alberto Mendoza Perez",
            DocumentType.CC,
            "3001234567",
            "carlos@email.com",
            "Calle 10 # 20-30"
        );

        Assert.Equal("3001234567", a.Phone);
        Assert.Equal("carlos@email.com", a.Email);
        Assert.Equal("Calle 10 # 20-30", a.Address);

        // Update Phone
        await bankingService.UpdateAssociatePhoneAsync("1020304050", "3159876543");
        var updatedPhone = await bankingService.GetAssociateByDocumentAsync("1020304050");
        Assert.Equal("3159876543", updatedPhone!.Phone);

        // Update Email
        await bankingService.UpdateAssociateEmailAsync("1020304050", "carlos.nuevo@email.com");
        var updatedEmail = await bankingService.GetAssociateByDocumentAsync("1020304050");
        Assert.Equal("carlos.nuevo@email.com", updatedEmail!.Email);

        // Update Address
        await bankingService.UpdateAssociateAddressAsync("1020304050", "Carrera 15 # 85-30, Bogotá");
        var updatedAddress = await bankingService.GetAssociateByDocumentAsync("1020304050");
        Assert.Equal("Carrera 15 # 85-30, Bogotá", updatedAddress!.Address);

        // Update Profile
        await bankingService.UpdateAssociateProfileAsync("1020304050", "Carlos Alberto Mendoza Gomez", "3201112233", "carlos.final@email.com", "Avenida 19 # 104-50, Cali");
        var updatedProfile = await bankingService.GetAssociateByDocumentAsync("1020304050");
        Assert.Equal("Carlos Alberto Mendoza Gomez", updatedProfile!.Name);
        Assert.Equal("3201112233", updatedProfile.Phone);
        Assert.Equal("carlos.final@email.com", updatedProfile.Email);
        Assert.Equal("Avenida 19 # 104-50, Cali", updatedProfile.Address);
    }
}
