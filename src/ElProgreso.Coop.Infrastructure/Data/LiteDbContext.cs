using LiteDB;
using ElProgreso.Coop.Domain.Entities;

namespace ElProgreso.Coop.Infrastructure.Data;

public class LiteDbContext : IDisposable
{
    public LiteDatabase Database { get; }

    public ILiteCollection<Associate> Associates => Database.GetCollection<Associate>("associates");
    public ILiteCollection<Transaction> Transactions => Database.GetCollection<Transaction>("transactions");

    public LiteDbContext(string connectionString = "elprogreso.db")
    {
        var mapper = new BsonMapper();

        mapper.Entity<Associate>()
            .Id(x => x.Document);

        mapper.Entity<Transaction>()
            .Id(x => x.Id);

        var conn = new ConnectionString(connectionString)
        {
            Connection = ConnectionType.Direct,
            Upgrade = true
        };

        Database = new LiteDatabase(conn, mapper);

        // Ensure indexes
        Associates.EnsureIndex(x => x.Document, unique: true);
        Transactions.EnsureIndex(x => x.AssociateDocument);
        Transactions.EnsureIndex(x => x.Date);
        Transactions.EnsureIndex(x => x.Amount);
    }

    public void Checkpoint()
    {
        Database.Checkpoint();
    }

    public void Dispose()
    {
        try
        {
            Database.Checkpoint();
        }
        catch
        {
            // Ignore checkpoint on disposal if already closed
        }
        Database.Dispose();
        GC.SuppressFinalize(this);
    }
}
