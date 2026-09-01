using ElProgreso.Coop.Domain.Entities;
using ElProgreso.Coop.Domain.Enums;

namespace ElProgreso.Coop.Application.Interfaces;

/// <summary>
/// Core application service orchestrating cashier banking operations and associate management.
/// </summary>
public interface IBankingService
{
    /// <summary>
    /// Registers a new associate in the cooperative with an initial zero-balance savings account.
    /// </summary>
    Task<Associate> RegisterAssociateAsync(
        string document,
        string name,
        DocumentType documentType = DocumentType.CC,
        string? phone = null,
        string? email = null,
        string? address = null);

    /// <summary>
    /// Updates the full name of an existing associate.
    /// </summary>
    Task<Associate> UpdateAssociateNameAsync(string document, string newName);

    /// <summary>
    /// Updates the contact phone number of an existing associate.
    /// </summary>
    Task<Associate> UpdateAssociatePhoneAsync(string document, string newPhone);

    /// <summary>
    /// Updates the email address of an existing associate.
    /// </summary>
    Task<Associate> UpdateAssociateEmailAsync(string document, string newEmail);

    /// <summary>
    /// Updates the residential address of an existing associate.
    /// </summary>
    Task<Associate> UpdateAssociateAddressAsync(string document, string newAddress);

    /// <summary>
    /// Updates all contact information fields (phone, email, address) for an associate.
    /// </summary>
    Task<Associate> UpdateAssociateContactInfoAsync(string document, string? phone, string? email, string? address);

    /// <summary>
    /// Updates both name and contact information for an associate.
    /// </summary>
    Task<Associate> UpdateAssociateProfileAsync(string document, string name, string? phone, string? email, string? address);

    /// <summary>
    /// Deletes an associate from the cooperative. Enforces the deletion guard rule forbidding removal if transactions exist.
    /// </summary>
    Task DeleteAssociateAsync(string document);

    /// <summary>
    /// Retrieves an associate by exact document number, hydrating ledger transactions to calculate balance.
    /// </summary>
    Task<Associate?> GetAssociateByDocumentAsync(string document);

    /// <summary>
    /// Performs a partial case-insensitive search by document number or full name.
    /// </summary>
    Task<IEnumerable<Associate>> SearchAssociatesAsync(string query);

    /// <summary>
    /// Retrieves all registered associates in the cooperative.
    /// </summary>
    Task<IEnumerable<Associate>> GetAllAssociatesAsync();

    /// <summary>
    /// Retrieves associates filtered by document type, balance status, or activity, with custom sorting.
    /// </summary>
    Task<IEnumerable<Associate>> GetFilteredAssociatesAsync(DTOs.AssociateFilterCriteria criteria);

    /// <summary>
    /// Processes a deposit into the associate's savings account.
    /// </summary>
    Task<Transaction> DepositAsync(string document, decimal amount);

    /// <summary>
    /// Processes a withdrawal, calculating handling commissions (> $1,000,000 COP) and preventing negative balances.
    /// </summary>
    Task<Transaction> WithdrawAsync(string document, decimal amount);

    /// <summary>
    /// Retrieves the chronological transaction history for an associate.
    /// </summary>
    Task<IEnumerable<Transaction>> GetAssociateTransactionsAsync(string document);
}
