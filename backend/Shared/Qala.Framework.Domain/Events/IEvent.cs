namespace Qala.Framework.Domain.Events;

/// <summary>Marker for integration events (ETOs) sent between modules over the message broker.</summary>
public interface IEvent
{
}

/// <summary>Publishes integration events. Implemented over MassTransit.</summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IEvent;
}
