using ElProgreso.Coop.Domain.Enums;
using ElProgreso.Coop.Domain.Exceptions;

namespace ElProgreso.Coop.Domain.Entities;

/// <summary>
/// Represents an immutable monetary movement (Deposit or Withdrawal) in an associate's account ledger.
/// </summary>
public class Transaction
{
    public const decimal HighWithdrawalThreshold = 1_000_000m;
    public const decimal WithdrawalCommissionFee = 8_000m;

    public Guid Id { get; private set; }
    public DateTime Date { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public decimal Commission { get; private set; }
    public string AssociateDocument { get; private set; } = string.Empty;

    /// <summary>
    /// Computes the net mathematical impact of this transaction on the account balance.
    /// Deposits add the amount; withdrawals subtract the amount plus the handling commission.
    /// </summary>
    public decimal TotalImpact => Type == TransactionType.Deposit 
        ? Amount 
        : -(Amount + Commission);

    /// <summary>
    /// Parameterless constructor for persistence/serialization.
    /// </summary>
    public Transaction()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Transaction"/> and calculates commissions if applicable.
    /// </summary>
    public Transaction(
        Guid id,
        DateTime date,
        TransactionType type,
        decimal amount,
        string associateDocument)
    {
        if (amount <= 0)
        {
            throw new InvalidTransactionAmountException($"Transaction amount must be strictly greater than zero. Received: {amount:N2}");
        }

        if (string.IsNullOrWhiteSpace(associateDocument))
        {
            throw new DomainException("Associate document is required for a transaction.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Date = date;
        Type = type;
        Amount = amount;
        AssociateDocument = associateDocument.Trim();

        // Automatic $8,000 commission for withdrawals > $1,000,000 COP
        Commission = CalculateCommission(Type, Amount);
    }

    /// <summary>
    /// Calculates the mandatory commission fee for a given transaction type and amount.
    /// </summary>
    /// <param name="type">The type of transaction.</param>
    /// <param name="amount">The transaction amount.</param>
    /// <returns>The commission fee amount (e.g., $8,000 for withdrawals > $1,000,000).</returns>
    public static decimal CalculateCommission(TransactionType type, decimal amount)
    {
        return (type == TransactionType.Withdrawal && amount > HighWithdrawalThreshold)
            ? WithdrawalCommissionFee
            : 0m;
    }
}
