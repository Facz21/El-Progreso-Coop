namespace ElProgreso.Coop.Domain.Exceptions;

public class InsufficientFundsException : DomainException
{
    public decimal CurrentBalance { get; }
    public decimal RequestedAmount { get; }
    public decimal Commission { get; }

    public InsufficientFundsException(decimal currentBalance, decimal requestedAmount, decimal commission)
        : base($"Insufficient funds. Current balance: {currentBalance:N2} COP. Total required (Amount: {requestedAmount:N2} + Fee: {commission:N2}): {(requestedAmount + commission):N2} COP.")
    {
        CurrentBalance = currentBalance;
        RequestedAmount = requestedAmount;
        Commission = commission;
    }
}
