using Microsoft.AspNetCore.SignalR;

namespace MidiController.Host.Hubs;

/// <summary>
/// Nachricht für ein einzelnes MIDI-Rohevent im Live-Log.
/// </summary>
public record MidiLogMessage(
    string  DeviceId,
    string  EventType,
    int     Channel,
    int     Data1,
    int     Data2,
    long    TimestampUs);

/// <summary>
/// SignalR-Hub für den MIDI-Rohdaten-Live-Log.
/// Clients können optional ein Gerät abonnieren; ohne Filter erhalten sie alle Events.
/// </summary>
public sealed class MidiLogHub : Hub
{
    private const string AllDevicesGroup  = "all-devices";
    private const string DeviceGroupPrefix = "device:";

    public override async Task OnConnectedAsync()
    {
        // Standardmäßig alle Events empfangen
        await Groups.AddToGroupAsync(Context.ConnectionId, AllDevicesGroup);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Client abonniert Events für ein bestimmtes Gerät (lässt AllDevices-Gruppe).
    /// </summary>
    public async Task SubscribeDevice(string deviceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, AllDevicesGroup);
        await Groups.AddToGroupAsync(Context.ConnectionId, DeviceGroupPrefix + deviceId);
    }

    /// <summary>
    /// Client wechselt zurück zu "alle Geräte".
    /// </summary>
    public async Task SubscribeAll()
    {
        // Alle device:-Gruppen verlassen ist nicht direkt möglich ohne Tracking;
        // SubscribeDevice überschreibt, SubscribeAll fügt wieder zur globalen Gruppe hinzu.
        await Groups.AddToGroupAsync(Context.ConnectionId, AllDevicesGroup);
    }

    // Statische Helfer für den Broadcaster
    internal static string GroupForDevice(string deviceId) => DeviceGroupPrefix + deviceId;
    internal static string AllGroup => AllDevicesGroup;
}
