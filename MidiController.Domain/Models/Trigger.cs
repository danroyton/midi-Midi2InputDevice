using MidiController.Domain.Enums;

namespace MidiController.Domain.Models;

/// <summary>
/// Vollständiger Trigger: bindet ein MIDI-Event an Prüfblöcke und eine Aktionsfolge.
/// </summary>
/// <param name="MatchMode">
/// Bestimmt, wie der vierte Matching-Parameter ausgewertet wird.
/// <see cref="TriggerMatchMode.Variable"/> = feuert bei jedem Data2-Wert.
/// Alle anderen Modi vergleichen den berechneten Wert mit <paramref name="MatchValue"/>.
/// </param>
/// <param name="MatchValue">
/// Zielwert für den Vergleich (nur relevant wenn MatchMode != Variable).
/// </param>
public record Trigger(
    string TriggerId,
    string DeviceId,
    MidiEventType EventType,
    int Channel,
    int? Data1Filter,                        // null = beliebiger Data1-Wert
    TriggerMatchMode MatchMode,              // vierter Matching-Parameter
    int MatchValue,                          // Zielwert (ignoriert bei MatchMode.Variable)
    StateAssignment[] GlobalPreAssignments,  // Schritt 0: immer vor Prüfblöcken ausgeführt
    ConditionBlock[] ConditionBlocks,        // 1..n, UND-verknüpft
    ActionBlock[] Actions,                   // 1..n, sequenziell ausgeführt
    StateAssignment[] GlobalPostAssignments, // immer nach allen Aktionen ausgeführt
    TriggerConfig? ElseConfig                // optional: Alternativpfad bei Fehlschlag
)
{
    /// <summary>Optionaler benutzerfreundlicher Anzeigename.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>MIDI-Befehle, die vor den Prüfblöcken gesendet werden.</summary>
    public MidiSendCommand[] GlobalPreMidiSend  { get; init; } = [];
    /// <summary>MIDI-Befehle, die nach allen Aktionen gesendet werden.</summary>
    public MidiSendCommand[] GlobalPostMidiSend { get; init; } = [];
};

/// <summary>
/// ELSE-Zweig eines Triggers: eigene Prüfblöcke + Aktionsfolge.
/// </summary>
public record TriggerConfig(
    ConditionBlock[] ConditionBlocks,
    ActionBlock[] Actions,
    StateAssignment[] GlobalPostAssignments
);
