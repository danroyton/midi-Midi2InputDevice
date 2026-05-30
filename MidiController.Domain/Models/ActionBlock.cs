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
