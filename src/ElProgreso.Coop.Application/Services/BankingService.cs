using ElProgreso.Coop.Application.Interfaces;
using ElProgreso.Coop.Application.Validation;
using ElProgreso.Coop.Domain.Entities;
using ElProgreso.Coop.Domain.Enums;
using ElProgreso.Coop.Domain.Exceptions;

namespace ElProgreso.Coop.Application.Services;

public class BankingService : IBankingService
{
    private readonly IAssociateRepository _associateRepository;
    private readonly ITransactionRepository _transactionRepository;

    public BankingService(
        IAssociateRepository associateRepository,
        ITransactionRepository transactionRepository)
    {
        _associateRepository = associateRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<Associate> RegisterAssociateAsync(
        string document,
        string name,
        DocumentType documentType = DocumentType.CC,
        string? phone = null,
        string? email = null,
        string? address = null)
    {
        var validation = AssociateValidator.Validate(documentType, document, name, phone, email, address);
        if (!validation.IsValid)
        {
            throw new DomainException(validation.ErrorMessage ?? "Validation error registering associate.");
        }

        var normalizedDocument = document.Trim();
        if (await _associateRepository.ExistsAsync(normalizedDocument))
        {
            throw new DomainException($"Ya existe un asociado registrado con el documento '{normalizedDocument}'.");
        }

        var associate = new Associate(
            normalizedDocument,
            name.Trim(),
            documentType,
            phone,
            email,
            address,
            DateTime.UtcNow
        );

        await _associateRepository.AddAsync(associate);
        return associate;
    }

    public async Task<Associate> UpdateAssociateNameAsync(string document, string newName)
    {
        var nameValidation = AssociateValidator.ValidateName(newName);
        if (!nameValidation.IsValid)
        {
            throw new DomainException(nameValidation.ErrorMessage ?? "Validation error updating associate name.");
        }

        var normalizedDocument = document.Trim();
        var associate = await _associateRepository.GetByDocumentAsync(normalizedDocument);
        if (associate == null)
        {
            throw new AssociateNotFoundException(normalizedDocument);
        }

        associate.UpdateName(newName);
        await _associateRepository.UpdateAsync(associate);
        return associate;
    }

    public async Task<Associate> UpdateAssociatePhoneAsync(string document, string newPhone)
    {
        var phoneValidation = AssociateValidator.ValidatePhone(newPhone);
        if (!phoneValidation.IsValid)
        {
            throw new DomainException(phoneValidation.ErrorMessage ?? "Validation error updating associate phone.");
        }

        var normalizedDocument = document.Trim();
        var associate = await _associateRepository.GetByDocumentAsync(normalizedDocument);
        if (associate == null)
        {
            throw new AssociateNotFoundException(normalizedDocument);
        }

        associate.UpdatePhone(newPhone);
        await _associateRepository.UpdateAsync(associate);
        return associate;
    }

    public async Task<Associate> UpdateAssociateEmailAsync(string document, string newEmail)
    {
        var emailValidation = AssociateValidator.ValidateEmail(newEmail);
        if (!emailValidation.IsValid)
        {
            throw new DomainException(emailValidation.ErrorMessage ?? "Validation error updating associate email.");
        }

        var normalizedDocument = document.Trim();
        var associate = await _associateRepository.GetByDocumentAsync(normalizedDocument);
        if (associate == null)
        {
            throw new AssociateNotFoundException(normalizedDocument);
        }

        associate.UpdateEmail(newEmail);
        await _associateRepository.UpdateAsync(associate);
        return associate;
    }

    public async Task<Associate> UpdateAssociateAddressAsync(string document, string newAddress)
    {
        var addressValidation = AssociateValidator.ValidateAddress(newAddress);
        if (!addressValidation.IsValid)
        {
            throw new DomainException(addressValidation.ErrorMessage ?? "Validation error updating associate address.");
        }

        var normalizedDocument = document.Trim();
        var associate = await _associateRepository.GetByDocumentAsync(normalizedDocument);
        if (associate == null)
        {
            throw new AssociateNotFoundException(normalizedDocument);
        }

        associate.UpdateAddress(newAddress);
        await _associateRepository.UpdateAsync(associate);
        return associate;
    }

    public async Task<Associate> UpdateAssociateContactInfoAsync(string document, string? phone, string? email, string? address)
    {
        if (phone != null)
        {
            var pVal = AssociateValidator.ValidatePhone(phone);
            if (!pVal.IsValid) throw new DomainException(pVal.ErrorMessage!);
        }

        if (email != null)
        {
            var eVal = AssociateValidator.ValidateEmail(email);
            if (!eVal.IsValid) throw new DomainException(eVal.ErrorMessage!);
        }

        if (address != null)
        {
            var aVal = AssociateValidator.ValidateAddress(address);
            if (!aVal.IsValid) throw new DomainException(aVal.ErrorMessage!);
        }

        var normalizedDocument = document.Trim();
        var associate = await _associateRepository.GetByDocumentAsync(normalizedDocument);
        if (associate == null)
        {
            throw new AssociateNotFoundException(normalizedDocument);
        }

        associate.UpdateContactInfo(phone, email, address);
        await _associateRepository.UpdateAsync(associate);
        return associate;
    }

    public async Task<Associate> UpdateAssociateProfileAsync(string document, string name, string? phone, string? email, string? address)
    {
        var nVal = AssociateValidator.ValidateName(name);
        if (!nVal.IsValid) throw new DomainException(nVal.ErrorMessage!);

        if (phone != null)
        {
            var pVal = AssociateValidator.ValidatePhone(phone);
            if (!pVal.IsValid) throw new DomainException(pVal.ErrorMessage!);
        }

        if (email != null)
        {
            var eVal = AssociateValidator.ValidateEmail(email);
            if (!eVal.IsValid) throw new DomainException(eVal.ErrorMessage!);
        }

        if (address != null)
        {
            var aVal = AssociateValidator.ValidateAddress(address);
            if (!aVal.IsValid) throw new DomainException(aVal.ErrorMessage!);
        }

        var normalizedDocument = document.Trim();
        var associate = await _associateRepository.GetByDocumentAsync(normalizedDocument);
        if (associate == null)
        {
            throw new AssociateNotFoundException(normalizedDocument);
        }

        associate.UpdateProfile(name, phone, email, address);
        await _associateRepository.UpdateAsync(associate);
        return associate;
    }

    public async Task DeleteAssociateAsync(string document)
    {
        var normalizedDocument = document.Trim();
        var associate = await _associateRepository.GetByDocumentAsync(normalizedDocument);
        if (associate == null)
        {
            throw new AssociateNotFoundException(normalizedDocument);
        }

        if (await _transactionRepository.HasTransactionsAsync(normalizedDocument))
        {
            throw new AssociateHasTransactionsException(normalizedDocument);
        }

        await _associateRepository.DeleteAsync(normalizedDocument);
    }

    public async Task<Associate?> GetAssociateByDocumentAsync(string document)
    {
        return await _associateRepository.GetByDocumentAsync(document.Trim());
    }

    public async Task<IEnumerable<Associate>> SearchAssociatesAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return await _associateRepository.GetAllAsync();
        }

        var trimmed = query.Trim();
        var byDoc = await _associateRepository.GetByDocumentAsync(trimmed);
        if (byDoc != null)
        {
            return new[] { byDoc };
        }

        return await _associateRepository.SearchByNameAsync(trimmed);
    }

    public async Task<IEnumerable<Associate>> GetAllAssociatesAsync()
    {
        return await _associateRepository.GetAllAsync();
    }

    public async Task<IEnumerable<Associate>> GetFilteredAssociatesAsync(DTOs.AssociateFilterCriteria criteria)
    {
        var query = await _associateRepository.GetAllAsync();

        if (criteria.DocumentType.HasValue)
        {
            query = query.Where(a => a.DocumentType == criteria.DocumentType.Value);
        }

        query = criteria.BalanceFilter switch
        {
            DTOs.BalanceFilter.PositiveBalance => query.Where(a => a.Balance > 0),
            DTOs.BalanceFilter.ZeroBalance => query.Where(a => a.Balance == 0),
            DTOs.BalanceFilter.ActiveWithTransactions => query.Where(a => a.Transactions.Count > 0),
            DTOs.BalanceFilter.InactiveDormant => query.Where(a => a.Transactions.Count == 0),
            _ => query
        };

        query = criteria.SortBy switch
        {
            DTOs.AssociateSortField.NameAsc => query.OrderBy(a => a.Name),
            DTOs.AssociateSortField.NameDesc => query.OrderByDescending(a => a.Name),
            DTOs.AssociateSortField.BalanceDesc => query.OrderByDescending(a => a.Balance),
            DTOs.AssociateSortField.BalanceAsc => query.OrderBy(a => a.Balance),
            DTOs.AssociateSortField.RegistrationDateDesc => query.OrderByDescending(a => a.RegistrationDate),
            DTOs.AssociateSortField.RegistrationDateAsc => query.OrderBy(a => a.RegistrationDate),
            DTOs.AssociateSortField.DocumentAsc => query.OrderBy(a => a.Document),
            DTOs.AssociateSortField.DocumentDesc => query.OrderByDescending(a => a.Document),
            _ => query.OrderBy(a => a.Name)
        };

        return query.ToList();
    }

    public async Task<Transaction> DepositAsync(string document, decimal amount)
    {
        var normalizedDocument = document.Trim();
        var associate = await _associateRepository.GetByDocumentAsync(normalizedDocument);
        if (associate == null)
        {
            throw new AssociateNotFoundException(normalizedDocument);
        }

        var transaction = associate.CreateDeposit(amount, DateTime.UtcNow);
        await _transactionRepository.AddAsync(transaction);
        return transaction;
    }

    public async Task<Transaction> WithdrawAsync(string document, decimal amount)
    {
        var normalizedDocument = document.Trim();
        var associate = await _associateRepository.GetByDocumentAsync(normalizedDocument);
        if (associate == null)
        {
            throw new AssociateNotFoundException(normalizedDocument);
        }

        var transaction = associate.CreateWithdrawal(amount, DateTime.UtcNow);
        await _transactionRepository.AddAsync(transaction);
        return transaction;
    }

    public async Task<IEnumerable<Transaction>> GetAssociateTransactionsAsync(string document)
    {
        var normalizedDocument = document.Trim();
        if (!await _associateRepository.ExistsAsync(normalizedDocument))
        {
            throw new AssociateNotFoundException(normalizedDocument);
        }

        return await _transactionRepository.GetByAssociateDocumentAsync(normalizedDocument);
    }
}
