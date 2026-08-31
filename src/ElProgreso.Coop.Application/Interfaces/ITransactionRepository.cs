using ElProgreso.Coop.Domain.Entities;

namespace ElProgreso.Coop.Application.Interfaces;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id);
    Task<IEnumerable<Transaction>> GetByAssociateDocumentAsync(string associateDocument);
    Task<IEnumerable<Transaction>> GetAllAsync();
    Task AddAsync(Transaction transaction);
    Task<bool> HasTransactionsAsync(string associateDocument);
}
