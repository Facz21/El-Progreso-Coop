using ElProgreso.Coop.Domain.Entities;

namespace ElProgreso.Coop.Application.Interfaces;

public interface IAssociateRepository
{
    Task<Associate?> GetByDocumentAsync(string document);
    Task<IEnumerable<Associate>> SearchByNameAsync(string namePattern);
    Task<IEnumerable<Associate>> GetAllAsync();
    Task AddAsync(Associate associate);
    Task UpdateAsync(Associate associate);
    Task DeleteAsync(string document);
    Task<bool> ExistsAsync(string document);
}
