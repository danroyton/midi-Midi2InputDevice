using Microsoft.AspNetCore.SignalR;
using MidiController.Host.Signalr;

namespace MidiController.Host.Hubs;

/// <summary>
/// Nachrichten-Typen, die der StatusHub an Clients sendet.
/// </summary>
public record VariableChangedMessage(char Variable, string Alias, int Value);

public record VariableSnapshotMessage(
    Dictionary<char, int> Variables,   // A–Z mit aktuellem Wert
    Dictionary<char, string> Aliases); // reservierte Variablen mit Alias

/// <summary>
/// SignalR-Hub für Live-Statusupdates der Zustandsvariablen A–Z.
/// Clients abonnieren automatisch beim Verbinden und erhalten sofort
/// einen vollständigen Snapshot.
/// </summary>
public sealed class StatusHub : Hub
{
    private readonly StatusBroadcaster _broadcaster;

    public StatusHub(StatusBroadcaster broadcaster)
    {
        _broadcaster = broadcaster;
    }

    /// <summary>
    /// Wird aufgerufen, sobald ein neuer Client verbunden ist.
    /// Sendet sofort den aktuellen Variablen-Snapshot.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var snapshot = _broadcaster.GetSnapshot();
        await Clients.Caller.SendAsync("VariableSnapshot", snapshot);
        await base.OnConnectedAsync();
    }
}
