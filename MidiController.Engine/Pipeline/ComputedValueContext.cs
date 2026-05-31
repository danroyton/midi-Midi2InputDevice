using MidiController.Domain.Models;

namespace MidiController.Engine.Pipeline;

/// <summary>
/// Fasst alle Laufzeitwerte zusammen, die während der Auswertung eines einzelnen MidiEvents
/// zur Verfügung stehen: das Event selbst, DeltaData2 und die davon abgeleiteten Lesewerte.
/// </summary>
public sealed class ComputedValueContext
{
    public MidiEvent Event { get; }

    /// <summary>Differenz von Data2 zum vorherigen Event unter demselben (Type, Channel, Data1)-Key. 0 beim ersten Auftreten.</summary>
    public int DeltaData2 { get; }

    /// <summary>Absoluter Betrag von DeltaData2, wenn Δ > 0; sonst 0.</summary>
    public int DD2Positive { get; }

    /// <summary>Absoluter Betrag von DeltaData2, wenn Δ &lt; 0; sonst 0.</summary>
    public int DD2Negative { get; }

    public ComputedValueContext(MidiEvent midiEvent, int deltaData2)
    {
        Event      = midiEvent;
        DeltaData2 = deltaData2;

        DD2Positive = deltaData2 > 0 ? deltaData2 : 0;
        DD2Negative = deltaData2 < 0 ? -deltaData2 : 0;
    }
}
