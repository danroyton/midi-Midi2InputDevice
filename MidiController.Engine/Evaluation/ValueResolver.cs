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

            ValueSource.DeltaData1  => ctx.DeltaData1,
            ValueSource.DeltaData2  => ctx.DeltaData2,

            ValueSource.DD1PosAbs   => ctx.DD1PosAbs,
            ValueSource.DD1NegAbs   => ctx.DD1NegAbs,
            ValueSource.DD1Pos      => ctx.DD1Pos,
            ValueSource.DD1Neg      => ctx.DD1Neg,

            ValueSource.DD2PosAbs   => ctx.DD2PosAbs,
            ValueSource.DD2NegAbs   => ctx.DD2NegAbs,
            ValueSource.DD2Pos      => ctx.DD2Pos,
            ValueSource.DD2Neg      => ctx.DD2Neg,

            >= ValueSource.VariableA and <= ValueSource.VariableZ
                => _variables.Get(VariableCharOf(source)),

            _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unbekannte ValueSource")
        };

    // ── Hilfsmethoden ────────────────────────────────────────────────────────

    private static char VariableCharOf(ValueSource source) =>
        (char)('A' + (source - ValueSource.VariableA));
}
