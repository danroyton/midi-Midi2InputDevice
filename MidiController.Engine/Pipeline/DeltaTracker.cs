using MidiController.Domain.Models;

namespace MidiController.Engine.Pipeline;

/// <summary>
/// Berechnet Delta-Werte für eingehende MIDI-Events:
/// - DeltaData2: Differenz von Data2 zum letzten Event unter demselben Triple-Key (Type, Channel, Data1).
///   Beim ersten Auftreten eines Keys wird kein Delta berechnet – es wird angenommen,
///   der vorherige Wert sei identisch mit dem aktuellen (Delta = 0).
/// </summary>
public sealed class DeltaTracker
{
    // Schlüssel: DeviceId|EventType|Channel|Data1  → letzter Data2-Wert
    private readonly Dictionary<string, int> _lastData2 = new();

    /// <summary>
    /// Verarbeitet ein eingehendes Event, aktualisiert den internen Zustand
    /// und gibt den berechneten <see cref="ComputedValueContext"/> zurück.
    /// </summary>
    public ComputedValueContext ComputeAndUpdate(MidiEvent midiEvent)
    {
        int deltaData2 = ComputeDeltaData2AndUpdate(midiEvent);
        return new ComputedValueContext(midiEvent, deltaData2);
    }

    // ── Hilfsmethoden ────────────────────────────────────────────────────────

    private int ComputeDeltaData2AndUpdate(MidiEvent e)
    {
        var key   = TripleKey(e);
        int prev  = _lastData2.TryGetValue(key, out var p) ? p : e.Data2;
        int delta = e.Data2 - prev;
        _lastData2[key] = e.Data2;
        return delta;
    }

    private static string TripleKey(MidiEvent e) =>
        $"{e.DeviceId}|{e.Type}|{e.Channel}|{e.Data1}";
}
