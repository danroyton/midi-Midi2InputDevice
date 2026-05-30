using MidiController.Domain.Enums;

namespace MidiController.Domain.Models;

/// <summary>
/// Vollständiger Trigger: bindet ein MIDI-Event an Prüfblöcke und eine Aktionsfolge.
/// </summary>
public record Trigger(
    string TriggerId,
    string DeviceId,
    MidiEventType EventType,
    int Channel,
    int? Data1Filter,                        // null = beliebiger Data1-Wert
    StateAssignment[] GlobalPreAssignments,  // Schritt 0: immer vor Prüfblöcken ausgeführt
    ConditionBlock[] ConditionBlocks,        // 1..n, UND-verknüpft
    ActionBlock[] Actions,                   // 1..n, sequenziell ausgeführt
    StateAssignment[] GlobalPostAssignments, // immer nach allen Aktionen ausgeführt
    TriggerConfig? ElseConfig                // optional: Alternativpfad bei Fehlschlag
);

/// <summary>
/// ELSE-Zweig eines Triggers: eigene Prüfblöcke + Aktionsfolge.
/// </summary>
public record TriggerConfig(
    ConditionBlock[] ConditionBlocks,
    ActionBlock[] Actions,
    StateAssignment[] GlobalPostAssignments
);
