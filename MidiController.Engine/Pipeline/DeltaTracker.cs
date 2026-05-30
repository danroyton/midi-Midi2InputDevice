using MidiController.Domain.Models;

namespace MidiController.Engine.Pipeline;

/// <summary>
/// Berechnet Delta-Werte für eingehende MIDI-Events:
/// - DeltaData1 (V): Differenz von Data1 zum letzten Event auf demselben DeviceId+EventType+Channel
/// - DeltaData2 (W): Differenz von Data2 zum letzten Event auf demselben DeviceId+EventType+Channel+Data1
/// </summary>
public sealed class DeltaTracker
{
    // Schlüssel für V: DeviceId|EventType|Channel  → letzter Data1-Wert
    private readonly Dictionary<string, int> _lastData1 = new();

    // Schlüssel für W: DeviceId|EventType|Channel|Data1  → letzter Data2-Wert
    private readonly Dictionary<string, int> _lastData2 = new();

    /// <summary>
    /// Verarbeitet ein eingehendes Event, aktualisiert den internen Zustand
    /// und gibt den berechneten <see cref="ComputedValueContext"/> zurück.
    /// </summary>
    public ComputedValueContext ComputeAndUpdate(MidiEvent midiEvent)
    {
        int deltaData1 = ComputeDeltaData1AndUpdate(midiEvent);
        int deltaData2 = ComputeDeltaData2AndUpdate(midiEvent);

        return new ComputedValueContext(midiEvent, deltaData1, deltaData2);
    }

    // ── Hilfsmethoden ────────────────────────────────────────────────────────

    private int ComputeDeltaData1AndUpdate(MidiEvent e)
    {
        var key    = ChannelKey(e);
        int prev   = _lastData1.TryGetValue(key, out var p) ? p : e.Data1;
        int delta  = e.Data1 - prev;
        _lastData1[key] = e.Data1;
        return delta;
    }

    private int ComputeDeltaData2AndUpdate(MidiEvent e)
    {
        var key    = ChannelAndData1Key(e);
        int prev   = _lastData2.TryGetValue(key, out var p) ? p : e.Data2;
        int delta  = e.Data2 - prev;
        _lastData2[key] = e.Data2;
        return delta;
    }

    private static string ChannelKey(MidiEvent e) =>
        $"{e.DeviceId}|{e.Type}|{e.Channel}";

    private static string ChannelAndData1Key(MidiEvent e) =>
        $"{e.DeviceId}|{e.Type}|{e.Channel}|{e.Data1}";
}
