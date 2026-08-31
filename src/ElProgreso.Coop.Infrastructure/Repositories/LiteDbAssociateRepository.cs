using ElProgreso.Coop.Application.Interfaces;
using ElProgreso.Coop.Domain.Entities;
using ElProgreso.Coop.Infrastructure.Data;

namespace ElProgreso.Coop.Infrastructure.Repositories;

public class LiteDbAssociateRepository : IAssociateRepository
{
    private readonly LiteDbContext _context;

    public LiteDbAssociateRepository(LiteDbContext context)
    {
        _context = context;
    }

    public Task<Associate?> GetByDocumentAsync(string document)
    {
        var associate = _context.Associates.FindById(document);
        if (associate != null)
        {
            var transactions = _context.Transactions
                .Find(t => t.AssociateDocument == associate.Document)
                .OrderBy(t => t.Date);
            associate.LoadTransactions(transactions);
        }

        return Task.FromResult(associate);
    }

    public Task<IEnumerable<Associate>> SearchByNameAsync(string namePattern)
    {
        var lowerPattern = namePattern.ToLowerInvariant();
        var associates = _context.Associates
            .FindAll()
            .Where(a => a.Name.ToLowerInvariant().Contains(lowerPattern))
            .ToList();

        HydrateTransactions(associates);
        return Task.FromResult<IEnumerable<Associate>>(associates);
    }

    public Task<IEnumerable<Associate>> GetAllAsync()
    {
        var associates = _context.Associates.FindAll().ToList();
        HydrateTransactions(associates);
        return Task.FromResult<IEnumerable<Associate>>(associates);
    }

    public Task AddAsync(Associate associate)
    {
        _context.Associates.Insert(associate);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Associate associate)
    {
        _context.Associates.Update(associate);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string document)
    {
        _context.Associates.Delete(document);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string document)
    {
        var exists = _context.Associates.Exists(x => x.Document == document);
        return Task.FromResult(exists);
    }

    private void HydrateTransactions(List<Associate> associates)
    {
        if (associates.Count == 0) return;

        var allTransactions = _context.Transactions.FindAll().ToList();
        var groupedTransactions = allTransactions
            .GroupBy(t => t.AssociateDocument)
            .ToDictionary(g => g.Key, g => g.OrderBy(t => t.Date).ToList());

        foreach (var associate in associates)
        {
            if (groupedTransactions.TryGetValue(associate.Document, out var txs))
            {
                associate.LoadTransactions(txs);
            }
            else
            {
                associate.LoadTransactions(Enumerable.Empty<Transaction>());
            }
        }
    }
}
