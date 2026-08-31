using ElProgreso.Coop.Domain.Enums;

namespace ElProgreso.Coop.Application.DTOs;

public record CooperativeOverviewReport(
    decimal TotalCooperativeBalance,
    int TotalAssociates,
    decimal AverageBalance
);

public record TopAssociateReportItem(
    string Document,
    string Name,
    decimal Balance,
    int TransactionCount
);

public record DormantAssociateReportItem(
    string Document,
    string Name,
    DateTime RegistrationDate
);

public record DateRangeSummaryReport(
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalDeposited,
    int DepositCount,
    decimal TotalWithdrawn,
    int WithdrawalCount,
    decimal TotalCommissions,
    decimal NetDifference,
    int TotalTransactions
);

public record LargestTransactionReportItem(
    Guid TransactionId,
    DateTime Date,
    TransactionType Type,
    decimal Amount,
    decimal Commission,
    string AssociateDocument,
    string AssociateName
);

public record CashierAssociateMovementReportItem(
    string AssociateDocument,
    string AssociateName,
    int TransactionCount,
    decimal TotalDeposited,
    decimal TotalWithdrawn,
    decimal TotalCommissions,
    decimal CurrentBalance
);
