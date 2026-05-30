using MidiController.Domain.State;
using MidiController.Engine.State;

namespace MidiController.Engine.Evaluation;

public enum GateResult
{
    /// <summary>A == 0: Verarbeitung läuft normal.</summary>
    Pass,
    /// <summary>A == 1: Prüfblöcke überspringen; nur A-setzende Aktionen ausführen.</summary>
    Paused,
    /// <summary>A == 2: Event komplett verwerfen.</summary>
    Blocked,
}

/// <summary>
/// Prüft den Wert der Gate-Variable A und bestimmt, wie weit ein MidiEvent verarbeitet wird.
/// </summary>
public sealed class GateEvaluator
{
    private readonly VariableStore _variables;

    public GateEvaluator(VariableStore variables)
    {
        _variables = variables;
    }

    public GateResult Evaluate() =>
        _variables.Get(EngineState.GateVariable) switch
        {
            EngineState.GateActive  => GateResult.Pass,
            EngineState.GatePaused  => GateResult.Paused,
            _                       => GateResult.Blocked,
        };
}
