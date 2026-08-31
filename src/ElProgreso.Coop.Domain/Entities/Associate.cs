using ElProgreso.Coop.Domain.Enums;
using ElProgreso.Coop.Domain.Exceptions;

namespace ElProgreso.Coop.Domain.Entities;

/// <summary>
/// Represents a cooperative member (Associate) with an associated savings account.
/// </summary>
public class Associate
{
    private readonly List<Transaction> _transactions = new();

    public string Document { get; private set; } = string.Empty;
    public DocumentType DocumentType { get; private set; } = DocumentType.CC;
    public string Name { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public DateTime RegistrationDate { get; private set; }

    /// <summary>
    /// Read-only collection of transactions recorded in the associate's savings account ledger.
    /// </summary>
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    /// <summary>
    /// Dynamically computed balance derived from the sum of all transaction impacts.
    /// Has no direct setter, preventing arbitrary manual modifications.
    /// </summary>
    public decimal Balance => _transactions.Sum(t => t.TotalImpact);

    /// <summary>
    /// Parameterless constructor required for BSON persistence/serialization.
    /// </summary>
    public Associate()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Associate"/> with basic identifiers.
    /// </summary>
    public Associate(
        string document,
        string name,
        DocumentType documentType,
        DateTime? registrationDate)
        : this(document, name, documentType, string.Empty, string.Empty, string.Empty, registrationDate)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Associate"/> with full contact details.
    /// </summary>
    public Associate(
        string document,
        string name,
        DocumentType documentType = DocumentType.CC,
        string? phone = null,
        string? email = null,
        string? address = null,
        DateTime? registrationDate = null)
    {
        if (string.IsNullOrWhiteSpace(document))
        {
            throw new DomainException("Associate document cannot be null or empty.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Associate name cannot be null or empty.");
        }

        Document = document.Trim();
        Name = name.Trim();
        DocumentType = documentType;
        Phone = phone?.Trim() ?? string.Empty;
        Email = email?.Trim() ?? string.Empty;
        Address = address?.Trim() ?? string.Empty;
        RegistrationDate = registrationDate ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the associate's full name.
    /// </summary>
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new DomainException("Associate name cannot be null or empty.");
        }

        Name = newName.Trim();
    }

    /// <summary>
    /// Updates the associate's phone number.
    /// </summary>
    public void UpdatePhone(string? newPhone)
    {
        Phone = newPhone?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Updates the associate's email address.
    /// </summary>
    public void UpdateEmail(string? newEmail)
    {
        Email = newEmail?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Updates the associate's residential address.
    /// </summary>
    public void UpdateAddress(string? newAddress)
    {
        Address = newAddress?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Updates all contact details (phone, email, and address).
    /// </summary>
    public void UpdateContactInfo(string? phone, string? email, string? address)
    {
        UpdatePhone(phone);
        UpdateEmail(email);
        UpdateAddress(address);
    }

    /// <summary>
    /// Updates full profile including name and contact details.
    /// </summary>
    public void UpdateProfile(string newName, string? phone, string? email, string? address)
    {
        UpdateName(newName);
        UpdateContactInfo(phone, email, address);
    }

    /// <summary>
    /// Loads historical transactions into the internal collection for ledger calculations.
    /// </summary>
    public void LoadTransactions(IEnumerable<Transaction> transactions)
    {
        _transactions.Clear();
        if (transactions != null)
        {
            _transactions.AddRange(transactions.Where(t => t.AssociateDocument == Document));
        }
    }

    /// <summary>
    /// Creates and applies a deposit transaction, crediting the account balance.
    /// </summary>
    /// <param name="amount">The positive monetary amount to deposit.</param>
    /// <param name="date">Optional transaction timestamp (defaults to UTC now).</param>
    /// <returns>The newly created <see cref="Transaction"/> entity.</returns>
    public Transaction CreateDeposit(decimal amount, DateTime? date = null)
    {
        if (amount <= 0)
        {
            throw new InvalidTransactionAmountException($"Deposit amount must be greater than zero. Received: {amount:N2}");
        }

        var transaction = new Transaction(
            Guid.NewGuid(),
            date ?? DateTime.UtcNow,
            TransactionType.Deposit,
            amount,
            Document
        );

        _transactions.Add(transaction);
        return transaction;
    }

    /// <summary>
    /// Creates and applies a withdrawal transaction, deducting funds and calculating applicable fees.
    /// Enforces the business rule that balance must never drop below zero.
    /// </summary>
    /// <param name="amount">The positive monetary amount to withdraw.</param>
    /// <param name="date">Optional transaction timestamp (defaults to UTC now).</param>
    /// <returns>The newly created <see cref="Transaction"/> entity.</returns>
    public Transaction CreateWithdrawal(decimal amount, DateTime? date = null)
    {
        if (amount <= 0)
        {
            throw new InvalidTransactionAmountException($"Withdrawal amount must be greater than zero. Received: {amount:N2}");
        }

        var commission = Transaction.CalculateCommission(TransactionType.Withdrawal, amount);
        var totalRequired = amount + commission;

        if (Balance < totalRequired)
        {
            throw new InsufficientFundsException(Balance, amount, commission);
        }

        var transaction = new Transaction(
            Guid.NewGuid(),
            date ?? DateTime.UtcNow,
            TransactionType.Withdrawal,
            amount,
            Document
        );

        _transactions.Add(transaction);
        return transaction;
    }
}
