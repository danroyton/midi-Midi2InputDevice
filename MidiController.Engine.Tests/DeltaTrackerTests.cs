using MidiController.Domain.Enums;
using MidiController.Domain.Models;
using MidiController.Engine.Pipeline;

namespace MidiController.Engine.Tests;

public class DeltaTrackerTests
{
    private static MidiEvent MakeEvent(int data1, int data2) =>
        new("dev1", MidiEventType.ControlChange, 1, data1, data2, 0);

    [Fact]
    public void FirstEvent_ProducesDeltaZero()
    {
        var tracker = new DeltaTracker();
        var ctx     = tracker.ComputeAndUpdate(MakeEvent(10, 64));
        Assert.Equal(0, ctx.DeltaData1);
        Assert.Equal(0, ctx.DeltaData2);
    }

    [Fact]
    public void SecondEvent_ProducesCorrectDeltas()
    {
        var tracker = new DeltaTracker();
        tracker.ComputeAndUpdate(MakeEvent(10, 64));
        // DeltaData1: Data1 ändert sich 10→15 = +5
        // DeltaData2: Data1 ist nun 15 (neuer Regler) → erstes Auftreten → Delta = 0
        var ctx = tracker.ComputeAndUpdate(MakeEvent(15, 70));
        Assert.Equal(5, ctx.DeltaData1);
        Assert.Equal(0, ctx.DeltaData2); // erster Wert für diesen Regler
    }

    [Fact]
    public void SecondEvent_SameData1_ProducesData2Delta()
    {
        var tracker = new DeltaTracker();
        tracker.ComputeAndUpdate(MakeEvent(10, 64));
        // Selber Regler (Data1=10), Data2 ändert sich 64→70 = +6
        var ctx = tracker.ComputeAndUpdate(MakeEvent(10, 70));
        Assert.Equal(0, ctx.DeltaData1); // Data1 unverändert
        Assert.Equal(6, ctx.DeltaData2);
    }

    [Fact]
    public void NegativeDelta_CalculatedCorrectly()
    {
        var tracker = new DeltaTracker();
        tracker.ComputeAndUpdate(MakeEvent(10, 64));
        // Selber Regler (Data1=10), Data2 64→60 = -4; Data1 10→5 = -5
        var ctx = tracker.ComputeAndUpdate(MakeEvent(5, 64));
        Assert.Equal(-5, ctx.DeltaData1);
        Assert.Equal(0,  ctx.DeltaData2); // neuer Data1-Key → erstes Auftreten
    }

    [Fact]
    public void DD1PosAbs_IsAbsoluteValue_WhenPositive()
    {
        var tracker = new DeltaTracker();
        tracker.ComputeAndUpdate(MakeEvent(0, 0));
        var ctx = tracker.ComputeAndUpdate(MakeEvent(7, 0));
        Assert.Equal(7, ctx.DD1PosAbs);
        Assert.Equal(0, ctx.DD1NegAbs);
        Assert.Equal(1, ctx.DD1Pos);
        Assert.Equal(0, ctx.DD1Neg);
    }

    [Fact]
    public void DD1NegAbs_IsAbsoluteValue_WhenNegative()
    {
        var tracker = new DeltaTracker();
        tracker.ComputeAndUpdate(MakeEvent(10, 0));
        var ctx = tracker.ComputeAndUpdate(MakeEvent(3, 0));
        Assert.Equal(0, ctx.DD1PosAbs);
        Assert.Equal(7, ctx.DD1NegAbs);
        Assert.Equal(0, ctx.DD1Pos);
        Assert.Equal(1, ctx.DD1Neg);
    }

    [Fact]
    public void AllDerivedValues_AreZero_WhenDeltaIsZero()
    {
        var tracker = new DeltaTracker();
        tracker.ComputeAndUpdate(MakeEvent(10, 64));
        // Identisches Event: beide Deltas = 0
        var ctx = tracker.ComputeAndUpdate(MakeEvent(10, 64));
        Assert.Equal(0, ctx.DD1PosAbs);
        Assert.Equal(0, ctx.DD1NegAbs);
        Assert.Equal(0, ctx.DD1Pos);
        Assert.Equal(0, ctx.DD1Neg);
        Assert.Equal(0, ctx.DD2PosAbs);
        Assert.Equal(0, ctx.DD2NegAbs);
        Assert.Equal(0, ctx.DD2Pos);
        Assert.Equal(0, ctx.DD2Neg);
    }
}
