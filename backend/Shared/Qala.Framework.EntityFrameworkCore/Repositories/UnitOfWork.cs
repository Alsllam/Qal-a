using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Qala.Framework.Domain.Entities;
using Qala.Framework.Domain.Repositories;
using Qala.Framework.Domain.Security;

namespace Qala.Framework.EntityFrameworkCore.Repositories;

/// <summary>
/// Saves the module's DbContext and fills the audit columns from <see cref="ICurrentUser"/>: creation, modification,
/// soft delete (a delete of an <see cref="ISoftDelete"/> becomes an update) and concurrency stamps.
/// </summary>
public sealed class UnitOfWork(DbContext dbContext, ICurrentUser currentUser, TimeProvider timeProvider) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditing();
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        // The InMemory provider used by tests has no transactions; treat the unit of work as the transaction there.
        if (_transaction is null && dbContext.Database.IsRelational())
        {
            _transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        }
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        await SaveChangesAsync(cancellationToken);
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        dbContext.ChangeTracker.Clear();
    }

    private void ApplyAuditing()
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var userId = currentUser.Id;
        foreach (var entry in dbContext.ChangeTracker.Entries().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity is ICreationAuditedObject created)
                    {
                        if (created.CreationTime == default)
                        {
                            created.CreationTime = now;
                        }

                        created.CreatorId ??= userId;
                    }

                    if (entry.Entity is IHasConcurrencyStamp addedStamp && string.IsNullOrEmpty(addedStamp.ConcurrencyStamp))
                    {
                        addedStamp.ConcurrencyStamp = Guid.NewGuid().ToString("N");
                    }

                    break;

                case EntityState.Modified:
                    SetModified(entry.Entity, now, userId);
                    break;

                case EntityState.Deleted when entry.Entity is ISoftDelete softDelete:
                    entry.State = EntityState.Modified;
                    softDelete.IsDeleted = true;
                    softDelete.DeletionTime = now;
                    softDelete.DeleterId = userId;
                    SetModified(entry.Entity, now, userId);
                    break;
            }
        }
    }

    private static void SetModified(object entity, DateTime now, Guid? userId)
    {
        if (entity is IModificationAuditedObject modified)
        {
            modified.LastModificationTime = now;
            modified.LastModifierId = userId;
        }

        if (entity is IHasConcurrencyStamp stamp)
        {
            stamp.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        }
    }
}
