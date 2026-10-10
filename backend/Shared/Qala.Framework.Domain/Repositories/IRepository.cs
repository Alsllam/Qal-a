using System.Linq.Expressions;
using Qala.Framework.Domain.Entities;
using Qala.Framework.Domain.Paging;

namespace Qala.Framework.Domain.Repositories;

/// <summary>Read-only access (no tracking). Use it for every query that does not change data.</summary>
public interface IReadOnlyRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includes);

    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includes);

    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    Task<long> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includes);

    /// <summary>
    /// One page of rows. <paramref name="sorting"/> is a dynamic LINQ string such as <c>"CreationTime Desc"</c>.
    /// </summary>
    Task<PagedResultDto<TEntity>> GetPagedListAsync(
        Expression<Func<TEntity, bool>>? predicate,
        int skipCount,
        int maxResultCount,
        string? sorting,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includes);
}

/// <summary>Write access (tracked entities). Pass <c>autoSave: true</c> for single-step operations.</summary>
public interface IRepository<TEntity, TKey> : IReadOnlyRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    /// <summary>Loads a tracked entity or throws <see cref="Exceptions.EntityNotFoundException"/>.</summary>
    Task<TEntity> GetAsync(TKey id, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includes);

    Task<TEntity> InsertAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default);

    Task<TEntity> UpdateAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default);

    /// <summary>Deletes an entity. <see cref="ISoftDelete"/> entities are soft deleted by the unit of work.</summary>
    Task DeleteAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default);
}

/// <summary>Saves changes (filling audit columns) and wraps multi-step operations in a transaction.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
