using MidiController.Domain.Enums;

namespace MidiController.Domain.Models;

/// <summary>
/// Weist einer persistenten Variable (A–Z) einen Wert aus einer ValueSource zu.
/// DD*-Quellen und reservierte Variablen werden vom VariableStore validiert.
/// </summary>
public record StateAssignment(
    char Variable,         // 'A'–'Z'
    ValueSource Source,
    int FixedValue         // wird nur ausgewertet wenn Source == Fixed
);

/// <summary>
/// Sendet einen MIDI-Befehl an ein bestimmtes Output-Gerät.
/// Wird als Pre/Post-Schritt in Triggern verwendet.
/// </summary>
public record MidiSendCommand(
    string  DeviceId,
    string  EventType,   // NoteOn | NoteOff | ControlChange | ProgramChange | PitchBend
    int     Channel,
    int     Data1,
    int     Data2
);

/// <summary>
/// Eine einzelne ausführbare Aktion: optionaler Tastendruck gefolgt von State-Änderungen.
/// Mehrere ActionBlocks pro Trigger werden sequenziell ausgeführt.
/// </summary>
public record ActionBlock(
    string? TemplateName,
    string[] KeyCombination,         // leer = kein Tastendruck, nur State-Änderung
    ValueSource XSource, int XFixed, // Repeat-Anzahl
    ValueSource YSource, int YFixed, // KeyDuration in ms
    ValueSource ZSource, int ZFixed, // Pause nach Tastendruck in ms
    StateAssignment[] StateAssignments  // Zuweisungen nach dem Tastendruck dieser Aktion
);
