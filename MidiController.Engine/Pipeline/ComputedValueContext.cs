using MidiController.Domain.Models;

namespace MidiController.Engine.Pipeline;

/// <summary>
/// Fasst alle Laufzeitwerte zusammen, die während der Auswertung eines einzelnen MidiEvents
/// zur Verfügung stehen: das Event selbst, Delta-Werte und alle davon abgeleiteten Lesewerte.
/// </summary>
public sealed class ComputedValueContext
{
    public MidiEvent Event { get; }

    // V / W – rohe Delta-Werte (können negativ sein)
    public int DeltaData1 { get; }
    public int DeltaData2 { get; }

    // DD1-Ableitungen
    public int DD1PosAbs { get; }  // |ΔData1| wenn Δ>0, sonst 0
    public int DD1NegAbs { get; }  // |ΔData1| wenn Δ<0, sonst 0
    public int DD1Pos    { get; }  // 1 wenn Δ>0, sonst 0
    public int DD1Neg    { get; }  // 1 wenn Δ<0, sonst 0

    // DD2-Ableitungen
    public int DD2PosAbs { get; }
    public int DD2NegAbs { get; }
    public int DD2Pos    { get; }
    public int DD2Neg    { get; }

    public ComputedValueContext(MidiEvent midiEvent, int deltaData1, int deltaData2)
    {
        Event      = midiEvent;
        DeltaData1 = deltaData1;
        DeltaData2 = deltaData2;

        DD1PosAbs = deltaData1 > 0 ? deltaData1 : 0;
        DD1NegAbs = deltaData1 < 0 ? -deltaData1 : 0;
        DD1Pos    = deltaData1 > 0 ? 1 : 0;
        DD1Neg    = deltaData1 < 0 ? 1 : 0;

        DD2PosAbs = deltaData2 > 0 ? deltaData2 : 0;
        DD2NegAbs = deltaData2 < 0 ? -deltaData2 : 0;
        DD2Pos    = deltaData2 > 0 ? 1 : 0;
        DD2Neg    = deltaData2 < 0 ? 1 : 0;
    }
}
