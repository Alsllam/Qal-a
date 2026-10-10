using MassTransit;
using Qala.Framework.Domain.Events;

namespace Qala.Framework.EntityFrameworkCore.Messaging;

/// <summary><see cref="IEventPublisher"/> over MassTransit (RabbitMQ in production, in-memory in tests).</summary>
public sealed class MassTransitEventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IEvent =>
        publishEndpoint.Publish(@event, cancellationToken);
}

/// <summary><c>MessageBroker</c> section. Credentials come from environment variables, never appsettings.</summary>
public sealed class MessageBrokerSettings
{
    public const string SectionName = "MessageBroker";

    /// <summary><c>RabbitMq</c> (default) or <c>InMemory</c> (single-process dev without RabbitMQ).</summary>
    public string Transport { get; set; } = "RabbitMq";

    public string Host { get; set; } = "localhost";

    public string VirtualHost { get; set; } = "/";

    public string Username { get; set; } = "guest";

    public string Password { get; set; } = "guest";
}
