using ElProgreso.Coop.Application.DTOs;
using ElProgreso.Coop.Application.Interfaces;
using ElProgreso.Coop.Domain.Enums;

namespace ElProgreso.Coop.Application.Services;

public class ManagementReportService : IManagementReportService
{
    private readonly IAssociateRepository _associateRepository;
    private readonly ITransactionRepository _transactionRepository;

    public ManagementReportService(
        IAssociateRepository associateRepository,
        ITransactionRepository transactionRepository)
    {
        _associateRepository = associateRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<CooperativeOverviewReport> GetCooperativeOverviewAsync()
    {
        var associates = (await _associateRepository.GetAllAsync()).ToList();
        var totalBalance = associates.Sum(a => a.Balance);
        var totalCount = associates.Count;
        var averageBalance = totalCount > 0 ? totalBalance / totalCount : 0m;

        return new CooperativeOverviewReport(totalBalance, totalCount, averageBalance);
    }

    public async Task<IEnumerable<TopAssociateReportItem>> GetTop5AssociatesByBalanceAsync()
    {
        var associates = await _associateRepository.GetAllAsync();
        return associates
            .OrderByDescending(a => a.Balance)
            .Take(5)
            .Select(a => new TopAssociateReportItem(
                a.Document,
                a.Name,
                a.Balance,
                a.Transactions.Count
            ))
            .ToList();
    }

    public async Task<IEnumerable<DormantAssociateReportItem>> GetDormantAssociatesAsync()
    {
        var associates = await _associateRepository.GetAllAsync();
        return associates
            .Where(a => a.Transactions.Count == 0)
            .OrderBy(a => a.RegistrationDate)
            .Select(a => new DormantAssociateReportItem(
                a.Document,
                a.Name,
                a.RegistrationDate
            ))
            .ToList();
    }

    public async Task<DateRangeSummaryReport> GetDateRangeSummaryAsync(DateTime startDate, DateTime endDate)
    {
        // Normalize range so startDate is start of day and endDate is end of day if time was not specified
        var normalizedStart = startDate.Date;
        var normalizedEnd = endDate.Date.AddDays(1).AddTicks(-1);

        var transactions = (await _transactionRepository.GetAllAsync())
            .Where(t => t.Date >= normalizedStart && t.Date <= normalizedEnd)
            .ToList();

        var deposits = transactions.Where(t => t.Type == TransactionType.Deposit).ToList();
        var withdrawals = transactions.Where(t => t.Type == TransactionType.Withdrawal).ToList();

        var totalDeposited = deposits.Sum(t => t.Amount);
        var totalWithdrawn = withdrawals.Sum(t => t.Amount);
        var totalCommissions = withdrawals.Sum(t => t.Commission);
        var netDifference = totalDeposited - (totalWithdrawn + totalCommissions);

        return new DateRangeSummaryReport(
            normalizedStart,
            normalizedEnd,
            totalDeposited,
            deposits.Count,
            totalWithdrawn,
            withdrawals.Count,
            totalCommissions,
            netDifference,
            transactions.Count
        );
    }

    public async Task<IEnumerable<LargestTransactionReportItem>> GetTop10LargestTransactionsAsync()
    {
        var transactions = await _transactionRepository.GetAllAsync();
        var associates = (await _associateRepository.GetAllAsync())
            .ToDictionary(a => a.Document, a => a.Name);

        return transactions
            .OrderByDescending(t => t.Amount)
            .Take(10)
            .Select(t => new LargestTransactionReportItem(
                t.Id,
                t.Date,
                t.Type,
                t.Amount,
                t.Commission,
                t.AssociateDocument,
                associates.TryGetValue(t.AssociateDocument, out var name) ? name : "Unknown"
            ))
            .ToList();
    }

    public async Task<IEnumerable<CashierAssociateMovementReportItem>> GetCashierMovementSummaryPerAssociateAsync()
    {
        var associates = await _associateRepository.GetAllAsync();

        return associates
            .Select(a =>
            {
                var deposits = a.Transactions.Where(t => t.Type == TransactionType.Deposit).Sum(t => t.Amount);
                var withdrawals = a.Transactions.Where(t => t.Type == TransactionType.Withdrawal).Sum(t => t.Amount);
                var commissions = a.Transactions.Where(t => t.Type == TransactionType.Withdrawal).Sum(t => t.Commission);

                return new CashierAssociateMovementReportItem(
                    a.Document,
                    a.Name,
                    a.Transactions.Count,
                    deposits,
                    withdrawals,
                    commissions,
                    a.Balance
                );
            })
            .OrderByDescending(x => x.TransactionCount)
            .ThenByDescending(x => x.CurrentBalance)
            .ToList();
    }
}
