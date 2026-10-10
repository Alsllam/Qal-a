using System.Net;

namespace Qala.Framework.Domain.Exceptions;

/// <summary>A business rule was broken. Mapped to 400. The message is already localized.</summary>
public class CustomValidationException : Exception
{
    public CustomValidationException()
    {
    }

    public CustomValidationException(string message)
        : base(message) => Messages = [message];

    public CustomValidationException(IEnumerable<string> messages)
        : base(string.Join(" ", messages)) => Messages = messages.ToList();

    public CustomValidationException(string message, Exception innerException)
        : base(message, innerException) => Messages = [message];

    public IReadOnlyList<string> Messages { get; } = [];
}

/// <summary>An entity was not found by id. Mapped to 404.</summary>
public class EntityNotFoundException : Exception
{
    public EntityNotFoundException()
    {
    }

    public EntityNotFoundException(string message)
        : base(message)
    {
    }

    public EntityNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public EntityNotFoundException(Type entityType, object? id)
        : base($"{entityType.Name} {id} was not found")
    {
        EntityType = entityType;
        Id = id;
    }

    public Type? EntityType { get; }

    public object? Id { get; }
}

/// <summary>A resource (not an entity by id) was not found. Mapped to 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException()
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>The caller is signed in but not allowed. Mapped to 403.</summary>
public class ForbiddenException : Exception
{
    public ForbiddenException()
    {
    }

    public ForbiddenException(string message)
        : base(message)
    {
    }

    public ForbiddenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>A dependency is down. Mapped to 503.</summary>
public class ServiceUnAvailableException : Exception
{
    public ServiceUnAvailableException()
    {
    }

    public ServiceUnAvailableException(string message)
        : base(message)
    {
    }

    public ServiceUnAvailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>An error whose (localized) message is safe to show, with an explicit status code.</summary>
public class UserFriendlyException : Exception
{
    public UserFriendlyException()
    {
    }

    public UserFriendlyException(string message)
        : base(message)
    {
    }

    public UserFriendlyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public UserFriendlyException(string message, HttpStatusCode httpStatusCode)
        : base(message) => HttpStatusCode = httpStatusCode;

    public HttpStatusCode HttpStatusCode { get; } = HttpStatusCode.BadRequest;
}
