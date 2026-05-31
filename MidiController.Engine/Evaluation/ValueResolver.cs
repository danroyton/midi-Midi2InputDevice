using MidiController.Domain.Enums;
using MidiController.Engine.Pipeline;
using MidiController.Engine.State;

namespace MidiController.Engine.Evaluation;

/// <summary>
/// Löst einen <see cref="ValueSource"/>-Wert zur Laufzeit auf,
/// indem er auf das aktuelle <see cref="ComputedValueContext"/> und den <see cref="VariableStore"/> zugreift.
/// </summary>
public sealed class ValueResolver
{
    private readonly VariableStore _variables;

    public ValueResolver(VariableStore variables)
    {
        _variables = variables;
    }

    /// <summary>
    /// Gibt den konkreten Integer-Wert für eine ValueSource zurück.
    /// </summary>
    /// <param name="source">Die Quelle.</param>
    /// <param name="fixedValue">Wird nur verwendet wenn <paramref name="source"/> == Fixed.</param>
    /// <param name="ctx">Der Kontext des aktuellen MidiEvents.</param>
    public int Resolve(ValueSource source, int fixedValue, ComputedValueContext ctx) =>
        source switch
        {
            ValueSource.Fixed       => fixedValue,

            ValueSource.MidiData1   => ctx.Event.Data1,
            ValueSource.MidiData2   => ctx.Event.Data2,

            ValueSource.DeltaData2  => ctx.DeltaData2,
            ValueSource.DD2Positive => ctx.DD2Positive,
            ValueSource.DD2Negative => ctx.DD2Negative,

            >= ValueSource.VariableA and <= ValueSource.VariableZ
                => _variables.Get(VariableCharOf(source)),

            _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unbekannte ValueSource")
        };

    // ── Hilfsmethoden ────────────────────────────────────────────────────────

    private static char VariableCharOf(ValueSource source) =>
        (char)('A' + (source - ValueSource.VariableA));
}
