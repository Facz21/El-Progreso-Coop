namespace ElProgreso.Coop.Domain.Exceptions;

public class InvalidTransactionAmountException : DomainException
{
    public InvalidTransactionAmountException(string message = "Transaction amount must be greater than zero.")
        : base(message)
    {
    }
}
