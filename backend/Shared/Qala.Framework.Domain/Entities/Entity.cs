namespace Qala.Framework.Domain.Entities;

/// <summary>Marker for every entity.</summary>
public interface IEntity
{
}

/// <summary>An entity with a typed primary key.</summary>
public interface IEntity<TKey> : IEntity
{
    TKey Id { get; }
}

/// <summary>Smallest entity base: just an identity.</summary>
public abstract class Entity<TKey> : IEntity<TKey>
{
    protected Entity()
    {
    }

    protected Entity(TKey id) => Id = id;

    public TKey Id { get; protected set; } = default!;

    public override string ToString() => $"[{GetType().Name} {Id}]";
}
