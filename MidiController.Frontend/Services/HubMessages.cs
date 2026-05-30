namespace MidiControllerFrontend.Services;

// ── Nachrichten-Records – spiegeln die Server-Side-Typen wider ───────────────

/// <summary>Einzelne Variablenänderung vom StatusHub.</summary>
public record VariableChangedMessage(char Variable, string Alias, int Value);

/// <summary>Vollständiger Snapshot aller Variablen beim Verbindungsaufbau.</summary>
public record VariableSnapshotMessage(
    Dictionary<char, int>    Variables,
    Dictionary<char, string> Aliases);

/// <summary>MIDI-Rohdaten-Event vom MidiLogHub.</summary>
public record MidiLogMessage(
    string DeviceId,
    string EventType,
    int    Channel,
    int    Data1,
    int    Data2,
    long   TimestampUs);
