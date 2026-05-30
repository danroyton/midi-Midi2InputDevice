namespace MidiController.Domain.Enums;

/// <summary>
/// Beschreibt, woher ein Wert für X/Y/Z-Zuweisungen, Bedingungen oder StateAssignments stammt.
/// DD*-Werte sind berechnete Lesewerte und dürfen nicht als Ziel einer StateAssignment genutzt werden.
/// </summary>
public enum ValueSource
{
    // Fester Wert aus der Konfiguration
    Fixed,

    // Felder des aktuellen MidiEvents
    MidiData1,
    MidiData2,

    // Delta-Werte (V / W): Differenz zum vorherigen Event desselben Typs/Kanals
    DeltaData1,
    DeltaData2,

    // Berechnete Ableitungen von DeltaData1 (schreibgeschützt)
    DD1PosAbs,   // |ΔData1| wenn Δ > 0, sonst 0
    DD1NegAbs,   // |ΔData1| wenn Δ < 0, sonst 0
    DD1Pos,      // 1 wenn ΔData1 > 0, sonst 0
    DD1Neg,      // 1 wenn ΔData1 < 0, sonst 0

    // Berechnete Ableitungen von DeltaData2 (schreibgeschützt)
    DD2PosAbs,   // |ΔData2| wenn Δ > 0, sonst 0
    DD2NegAbs,   // |ΔData2| wenn Δ < 0, sonst 0
    DD2Pos,      // 1 wenn ΔData2 > 0, sonst 0
    DD2Neg,      // 1 wenn ΔData2 < 0, sonst 0

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
