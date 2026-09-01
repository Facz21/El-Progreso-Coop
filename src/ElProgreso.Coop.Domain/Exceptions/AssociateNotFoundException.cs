namespace ElProgreso.Coop.Domain.Exceptions;

public class AssociateNotFoundException : DomainException
{
    public string Document { get; }

    public AssociateNotFoundException(string document)
        : base($"Associate with document '{document}' was not found.")
    {
        Document = document;
    }
}
