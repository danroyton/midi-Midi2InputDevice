namespace MidiController.Domain.State;

/// <summary>
/// Konstanten und Metadaten für den Zustandsraum der MappingEngine.
/// </summary>
public static class EngineState
{
    public const int VarMin = -127;
    public const int VarMax = 127;

    public const char GateVariable = 'A';
    public const int GateActive  = 0;
    public const int GatePaused  = 1;
    public const int GateBlocked = 2;

    /// <summary>
    /// Sprechende Alias-Namen für reservierte Variablen (A, V–Z).
    /// Wird in der UI und im SignalR-Event VariableChanged genutzt.
    /// </summary>
    public static readonly IReadOnlyDictionary<char, string> Aliases =
        new Dictionary<char, string>
        {
            ['A'] = "ActiveListen",
            ['V'] = "DeltaData1",
            ['W'] = "DeltaData2",
            ['X'] = "Repeat",
            ['Y'] = "KeyDuration",
            ['Z'] = "Pause",
        };

    /// <summary>
    /// Initialwerte der Variablen. Nicht aufgeführte Variablen starten mit 0.
    /// </summary>
    public static readonly IReadOnlyDictionary<char, int> InitialValues =
        new Dictionary<char, int>
        {
            ['A'] = GateBlocked, // Sicherer Start: keine Verarbeitung bis zur expliziten Aktivierung
            ['X'] = 1,           // Repeat-Default: 1 Wiederholung
        };

    /// <summary>
    /// Variablen, die nicht frei beschreibbar sind (nur über definierte Mechanismen).
    /// </summary>
    public static readonly IReadOnlySet<char> ReservedVariables =
        new HashSet<char> { 'A', 'V', 'W' };
}
