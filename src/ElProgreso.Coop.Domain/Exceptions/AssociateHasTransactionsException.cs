namespace ElProgreso.Coop.Domain.Exceptions;

public class AssociateHasTransactionsException : DomainException
{
    public string Document { get; }

    public AssociateHasTransactionsException(string document)
        : base($"Associate with document '{document}' cannot be deleted because they have registered transactions.")
    {
        Document = document;
    }
}
