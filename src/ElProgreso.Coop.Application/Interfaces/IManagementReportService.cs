using ElProgreso.Coop.Application.DTOs;

namespace ElProgreso.Coop.Application.Interfaces;

/// <summary>
/// Generates financial and operational intelligence reports for cooperative management.
/// </summary>
public interface IManagementReportService
{
    /// <summary>
    /// Report 1: "¿Cuánta plata tenemos?" - Total cooperative savings, associate count, and average balance.
    /// </summary>
    Task<CooperativeOverviewReport> GetCooperativeOverviewAsync();

    /// <summary>
    /// Report 2: "¿Quiénes son mis mejores asociados?" - Top 5 associates with the highest balances descending.
    /// </summary>
    Task<IEnumerable<TopAssociateReportItem>> GetTop5AssociatesByBalanceAsync();

    /// <summary>
    /// Report 3: "¿Quiénes están dormidos?" - List of associates with zero recorded transactions since registration.
    /// </summary>
    Task<IEnumerable<DormantAssociateReportItem>> GetDormantAssociatesAsync();

    /// <summary>
    /// Report 4: "¿Cómo nos fue en un periodo?" - Date range summary of deposits, withdrawals, commissions, and net cash flow.
    /// </summary>
    Task<DateRangeSummaryReport> GetDateRangeSummaryAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Report 5: "¿Cuáles fueron los movimientos más grandes?" - Top 10 largest financial transactions cooperative-wide.
    /// </summary>
    Task<IEnumerable<LargestTransactionReportItem>> GetTop10LargestTransactionsAsync();

    /// <summary>
    /// Report 6: "¿Quién me está moviendo la caja?" - Movement summary per associate ordered by transaction count descending.
    /// </summary>
    Task<IEnumerable<CashierAssociateMovementReportItem>> GetCashierMovementSummaryPerAssociateAsync();
}
