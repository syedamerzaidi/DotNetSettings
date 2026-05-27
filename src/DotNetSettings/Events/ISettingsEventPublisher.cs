namespace DotNetSettings.Events;

/// <summary>
/// Publishes settings lifecycle events. Register a custom implementation to
/// integrate with MediatR, a message bus, or any other event infrastructure.
/// </summary>
public interface ISettingsEventPublisher
{
    /// <summary>Publishes <paramref name="event"/> to all interested handlers.</summary>
    Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : ISettingsEvent;
}
