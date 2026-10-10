using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qala.Framework.Domain.Entities;

namespace Qala.Framework.EntityFrameworkCore.Configurations;

/// <summary>
/// Base for every entity configuration: soft-delete query filter, concurrency stamp and audit column defaults.
/// <see cref="IActivableEntity.IsActive"/> gets no database default on purpose: a bool default would make EF skip an
/// explicit <c>false</c> on insert. Entities set <c>IsActive = true</c> in their constructor instead.
/// </summary>
public abstract class DefaultEntityTypeConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : class, IEntity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
        {
            var parameter = Expression.Parameter(typeof(TEntity), "x");
            var isDeleted = Expression.Property(Expression.Convert(parameter, typeof(ISoftDelete)), nameof(ISoftDelete.IsDeleted));
            builder.HasQueryFilter(Expression.Lambda<Func<TEntity, bool>>(Expression.Not(isDeleted), parameter));
            builder.HasIndex(nameof(ISoftDelete.IsDeleted));
        }

        if (typeof(IHasConcurrencyStamp).IsAssignableFrom(typeof(TEntity)))
        {
            builder.Property(nameof(IHasConcurrencyStamp.ConcurrencyStamp)).IsConcurrencyToken().HasMaxLength(40);
        }
    }
}
