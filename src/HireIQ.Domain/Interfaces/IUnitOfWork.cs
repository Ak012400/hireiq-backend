namespace HireIQ.Domain.Interfaces;

/// <summary>
/// Unit of Work — commits all repository changes in a single transaction.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
