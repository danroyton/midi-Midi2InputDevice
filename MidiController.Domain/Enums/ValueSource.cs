namespace MidiController.Domain.Enums;

/// <summary>
/// Beschreibt, woher ein Wert für X/Y/Z-Zuweisungen, Bedingungen oder StateAssignments stammt.
/// DD2-Werte sind berechnete Lesewerte und dürfen nicht als Ziel einer StateAssignment genutzt werden.
/// </summary>
public enum ValueSource
{
    // Fester Wert aus der Konfiguration
    Fixed,

    // Felder des aktuellen MidiEvents
    MidiData1,
    MidiData2,

    // DeltaData2: Differenz von Data2 zum vorherigen Event unter (Type, Channel, Data1)
    // Erster Treffer: Delta = 0 (Annahme: vorheriger Wert == aktueller Wert)
    DeltaData2,

    // Berechnete Ableitungen von DeltaData2 (schreibgeschützt)
    DD2Positive,  // |ΔData2| wenn Δ > 0, sonst 0
    DD2Negative,  // |ΔData2| wenn Δ < 0, sonst 0

    // Persistente Zustandsvariablen A–Z
    VariableA,
    VariableB,
    VariableC,
    VariableD,
    VariableE,
    VariableF,
    VariableG,
    VariableH,
    VariableI,
    VariableJ,
    VariableK,
    VariableL,
    VariableM,
    VariableN,
    VariableO,
    VariableP,
    VariableQ,
    VariableR,
    VariableS,
    VariableT,
    VariableU,
    VariableV,
    VariableW,
    VariableX,
    VariableY,
    VariableZ,
}
