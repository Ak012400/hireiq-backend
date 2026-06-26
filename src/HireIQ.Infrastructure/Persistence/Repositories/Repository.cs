using System.Linq.Expressions;
using HireIQ.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HireIQ.Infrastructure.Persistence.Repositories;

public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected readonly AppDbContext Db;
    protected readonly DbSet<TEntity> Set;

    public Repository(AppDbContext db)
    {
        Db = db;
        Set = db.Set<TEntity>();
    }

    public virtual Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Set.FindAsync(new object[] { id }, ct).AsTask();

    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken ct = default) =>
        await Set.AsNoTracking().ToListAsync(ct);

    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default) =>
        await Set.AsNoTracking().Where(predicate).ToListAsync(ct);

    public virtual Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(predicate, ct);

    public virtual Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default) =>
        Set.AnyAsync(predicate, ct);

    public virtual async Task AddAsync(TEntity entity, CancellationToken ct = default) =>
        await Set.AddAsync(entity, ct);

    public virtual void Update(TEntity entity) => Set.Update(entity);
    public virtual void Remove(TEntity entity) => Set.Remove(entity);
}
