using Microsoft.AspNetCore.SignalR;
using MidiController.Domain.State;
using MidiController.Engine.State;
using MidiController.Host.Hubs;

namespace MidiController.Host.Signalr;

/// <summary>
/// Singleton-Dienst, der auf <see cref="VariableStore.VariableChanged"/> lauscht
/// und Änderungen sowie Snapshots über den <see cref="StatusHub"/> broadcastet.
/// Wird beim Start registriert und hält den Hub-Kontext.
/// </summary>
public sealed class StatusBroadcaster
{
    private readonly IHubContext<StatusHub> _hub;
    private readonly VariableStore          _store;

    public StatusBroadcaster(IHubContext<StatusHub> hub, VariableStore store)
    {
        _hub   = hub;
        _store = store;

        // Auf Änderungen abonnieren – fire-and-forget per async void helper
        _store.VariableChanged += OnVariableChanged;
    }

    /// <summary>
    /// Gibt einen vollständigen Snapshot für neue Clients zurück.
    /// </summary>
    public VariableSnapshotMessage GetSnapshot()
    {
        var values = _store.Snapshot();
        return new VariableSnapshotMessage(
            Variables: new Dictionary<char, int>(values),
            Aliases:   new Dictionary<char, string>(EngineState.Aliases));
    }

    // ── Privat ───────────────────────────────────────────────────────────────

    private void OnVariableChanged(char variable, int value)
    {
        var alias = EngineState.Aliases.TryGetValue(variable, out var a) ? a : variable.ToString();
        var msg   = new VariableChangedMessage(variable, alias, value);

        // Nicht auf den Task warten – Broadcast darf nicht blockieren
        _ = _hub.Clients.All.SendAsync("VariableChanged", msg);
    }
}
