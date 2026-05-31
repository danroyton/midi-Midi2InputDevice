namespace MidiController.Domain.Enums;

/// <summary>
/// Legt fest, wie der vierte Matching-Parameter eines Triggers ausgewertet wird.
/// Channel, EventType und Data1 werden immer exakt verglichen.
/// </summary>
public enum TriggerMatchMode
{
    /// <summary>Trigger feuert bei jedem Data2-Wert (kein Data2-Filter).</summary>
    Variable,

    /// <summary>Trigger feuert nur wenn Data2 == MatchValue.</summary>
    Data2,

    /// <summary>Trigger feuert nur wenn DeltaData2 == MatchValue.</summary>
    DeltaData2,

    /// <summary>Trigger feuert nur wenn DD2Positive (|Δ| bei Δ&gt;0) == MatchValue.</summary>
    DD2Positive,

    /// <summary>Trigger feuert nur wenn DD2Negative (|Δ| bei Δ&lt;0) == MatchValue.</summary>
    DD2Negative,
}
