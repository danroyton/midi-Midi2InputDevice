using Microsoft.AspNetCore.SignalR;
using MidiController.Engine;
using MidiController.Host.Hubs;

namespace MidiController.Host.Signalr;

/// <summary>
/// Singleton-Dienst, der auf <see cref="MappingEngine.MidiEventReceived"/> lauscht
/// und jedes MIDI-Event über den <see cref="MidiLogHub"/> broadcastet:
/// - an die Gruppe "all-devices" (Clients ohne Gerätefilter)
/// - an die Gruppe "device:{DeviceId}" (Clients mit Gerätefilter)
/// </summary>
public sealed class MidiEventBroadcaster
{
    private readonly IHubContext<MidiLogHub> _hub;

    public MidiEventBroadcaster(IHubContext<MidiLogHub> hub, MappingEngine engine)
    {
        _hub = hub;
        engine.MidiEventReceived += OnMidiEvent;
    }

    // ── Privat ───────────────────────────────────────────────────────────────

    private void OnMidiEvent(Domain.Models.MidiEvent e)
    {
        var msg = new MidiLogMessage(
            DeviceId:    e.DeviceId,
            EventType:   e.Type.ToString(),
            Channel:     e.Channel,
            Data1:       e.Data1,
            Data2:       e.Data2,
            TimestampUs: e.TimestampUs);

        // An alle Clients ohne Gerätefilter + Clients die dieses Gerät abonniert haben
        _ = Task.WhenAll(
            _hub.Clients.Group(MidiLogHub.AllGroup).SendAsync("MidiEventReceived", msg),
            _hub.Clients.Group(MidiLogHub.GroupForDevice(e.DeviceId)).SendAsync("MidiEventReceived", msg));
    }
}
