using ElProgreso.Coop.Application.Interfaces;
using ElProgreso.Coop.Domain.Entities;
using ElProgreso.Coop.Infrastructure.Data;

namespace ElProgreso.Coop.Infrastructure.Repositories;

public class LiteDbTransactionRepository : ITransactionRepository
{
    private readonly LiteDbContext _context;

    public LiteDbTransactionRepository(LiteDbContext context)
    {
        _context = context;
    }

    public Task<Transaction?> GetByIdAsync(Guid id)
    {
        var tx = _context.Transactions.FindById(id);
        return Task.FromResult<Transaction?>(tx);
    }

    public Task<IEnumerable<Transaction>> GetByAssociateDocumentAsync(string associateDocument)
    {
        var transactions = _context.Transactions
            .Find(x => x.AssociateDocument == associateDocument)
            .OrderByDescending(x => x.Date)
            .AsEnumerable();

        return Task.FromResult(transactions);
    }

    public Task<IEnumerable<Transaction>> GetAllAsync()
    {
        var transactions = _context.Transactions
            .FindAll()
            .OrderByDescending(x => x.Date)
            .AsEnumerable();

        return Task.FromResult(transactions);
    }

    public Task AddAsync(Transaction transaction)
    {
        _context.Transactions.Insert(transaction);
        return Task.CompletedTask;
    }

    public Task<bool> HasTransactionsAsync(string associateDocument)
    {
        var exists = _context.Transactions.Exists(x => x.AssociateDocument == associateDocument);
        return Task.FromResult(exists);
    }
}
