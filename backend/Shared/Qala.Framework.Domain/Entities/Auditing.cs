namespace Qala.Framework.Domain.Entities;

/// <summary>Creation audit columns, filled by the unit of work.</summary>
public interface ICreationAuditedObject
{
    Guid? CreatorId { get; set; }

    DateTime CreationTime { get; set; }
}

/// <summary>Modification audit columns, filled by the unit of work.</summary>
public interface IModificationAuditedObject
{
    Guid? LastModifierId { get; set; }

    DateTime? LastModificationTime { get; set; }
}

/// <summary>Soft delete: the unit of work turns a delete into an update of these columns.</summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }

    Guid? DeleterId { get; set; }

    DateTime? DeletionTime { get; set; }
}

/// <summary>Anything that can be enabled or disabled (e.g. a banned player).</summary>
public interface IActivableEntity
{
    bool IsActive { get; set; }
}

/// <summary>Optimistic concurrency: the unit of work renews the stamp on every update.</summary>
public interface IHasConcurrencyStamp
{
    string ConcurrencyStamp { get; set; }
}

public abstract class CreationAuditedEntity<TKey> : Entity<TKey>, ICreationAuditedObject
{
    protected CreationAuditedEntity()
    {
    }

    protected CreationAuditedEntity(TKey id)
        : base(id)
    {
    }

    public Guid? CreatorId { get; set; }

    public DateTime CreationTime { get; set; }
}

public abstract class AuditedEntity<TKey> : CreationAuditedEntity<TKey>, IModificationAuditedObject
{
    protected AuditedEntity()
    {
    }

    protected AuditedEntity(TKey id)
        : base(id)
    {
    }

    public Guid? LastModifierId { get; set; }

    public DateTime? LastModificationTime { get; set; }
}

/// <summary>Default base for business data: full audit plus soft delete.</summary>
public abstract class FullAuditedEntity<TKey> : AuditedEntity<TKey>, ISoftDelete
{
    protected FullAuditedEntity()
    {
    }

    protected FullAuditedEntity(TKey id)
        : base(id)
    {
    }

    public bool IsDeleted { get; set; }

    public Guid? DeleterId { get; set; }

    public DateTime? DeletionTime { get; set; }
}
