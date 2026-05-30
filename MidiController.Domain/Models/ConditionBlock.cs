using MidiController.Domain.Enums;

namespace MidiController.Domain.Models;

/// <summary>
/// Eine einzelne Bedingung innerhalb eines ConditionBlock.
/// Left op Right, wobei Right entweder aus einer ValueSource oder einem festen Wert stammt.
/// </summary>
public record Condition(
    ValueSource Left,
    string Op,             // "==", "!=", "<", ">", "<=", ">="
    ValueSource RightSource,
    int RightFixed         // wird nur ausgewertet wenn RightSource == Fixed
);

/// <summary>
/// Ein Prüfblock mit ODER-verknüpften Bedingungen.
/// Mindestens eine Bedingung muss wahr sein, damit der Block wahr ist.
/// Wird per TemplateName referenziert oder inline definiert.
/// </summary>
public record ConditionBlock(
    string? TemplateName,      // null = inline; sonst Verweis auf gespeichertes Template
    Condition[] Conditions     // ODER-verknüpft
);
