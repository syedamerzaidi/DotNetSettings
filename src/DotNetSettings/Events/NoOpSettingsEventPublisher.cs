namespace DotNetSettings.Events;

/// <summary>Default no-op event publisher — events are opt-in.</summary>
internal sealed class NoOpSettingsEventPublisher : ISettingsEventPublisher
{
    public Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : ISettingsEvent
        => Task.CompletedTask;
}
