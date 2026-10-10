using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Qala.Framework.Domain.Constants;
using Qala.Framework.Domain.Entities;
using Qala.Framework.Domain.Exceptions;
using Qala.Framework.Domain.Paging;
using Qala.Framework.Domain.Repositories;

namespace Qala.Framework.EntityFrameworkCore.Repositories;

/// <summary>Read-only repository over the module's DbContext. Queries are <c>AsNoTracking</c>.</summary>
public class ReadOnlyRepository<TEntity, TKey>(DbContext dbContext) : IReadOnlyRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    protected DbContext DbContext { get; } = dbContext;

    protected DbSet<TEntity> DbSet => DbContext.Set<TEntity>();

    /// <summary>The base query: untracked here, tracked in <see cref="Repository{TEntity,TKey}"/>.</summary>
    protected virtual IQueryable<TEntity> Query => DbSet.AsNoTracking();

    public virtual Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includes) =>
        WithIncludes(Query, includes).FirstOrDefaultAsync(IdEquals(id), cancellationToken);

    public virtual Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includes) =>
        WithIncludes(Query, includes).FirstOrDefaultAsync(predicate, cancellationToken);

    public virtual Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
        Query.AnyAsync(predicate, cancellationToken);

    public virtual Task<long> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default) =>
        predicate is null ? Query.LongCountAsync(cancellationToken) : Query.LongCountAsync(predicate, cancellationToken);

    public virtual Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includes)
    {
        var query = WithIncludes(Query, includes);
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        return query.ToListAsync(cancellationToken);
    }

    public virtual async Task<PagedResultDto<TEntity>> GetPagedListAsync(
        Expression<Func<TEntity, bool>>? predicate,
        int skipCount,
        int maxResultCount,
        string? sorting,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includes)
    {
        var query = WithIncludes(Query, includes);
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        var total = await query.LongCountAsync(cancellationToken);
        var sorted = query.OrderBy(string.IsNullOrWhiteSpace(sorting) ? FieldDefinitions.DefaultSorting : sorting);
        var items = await sorted
            .Skip(Math.Max(0, skipCount))
            .Take(Math.Clamp(maxResultCount, 1, FieldDefinitions.MaxPageSize))
            .ToListAsync(cancellationToken);
        return new PagedResultDto<TEntity>(items, total);
    }

    protected static IQueryable<TEntity> WithIncludes(IQueryable<TEntity> query, Expression<Func<TEntity, object>>[] includes) =>
        includes.Aggregate(query, (current, include) => current.Include(include));

    protected static Expression<Func<TEntity, bool>> IdEquals(TKey id)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var holder = new IdHolder(id);
        var body = Expression.Equal(
            Expression.Property(parameter, nameof(IEntity<TKey>.Id)),
            Expression.Property(Expression.Constant(holder), nameof(IdHolder.Id)));
        return Expression.Lambda<Func<TEntity, bool>>(body, parameter);
    }

    /// <summary>Wraps the id so EF parameterizes it instead of inlining a constant.</summary>
    private sealed record IdHolder(TKey Id);
}

/// <summary>Write repository: tracked queries, insert/update/delete with optional auto-save through the unit of work.</summary>
public class Repository<TEntity, TKey>(DbContext dbContext, IUnitOfWork unitOfWork)
    : ReadOnlyRepository<TEntity, TKey>(dbContext), IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    protected override IQueryable<TEntity> Query => DbSet;

    public virtual async Task<TEntity> GetAsync(TKey id, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includes) =>
        await FindAsync(id, cancellationToken, includes) ?? throw new EntityNotFoundException(typeof(TEntity), id);

    public virtual async Task<TEntity> InsertAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
        if (autoSave)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return entity;
    }

    public virtual async Task<TEntity> UpdateAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        if (DbContext.Entry(entity).State == EntityState.Detached)
        {
            DbSet.Update(entity);
        }

        if (autoSave)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return entity;
    }

    public virtual async Task DeleteAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        DbSet.Remove(entity);
        if (autoSave)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
