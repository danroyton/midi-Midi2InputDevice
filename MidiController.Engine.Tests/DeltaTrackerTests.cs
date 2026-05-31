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
        Assert.Equal(0, ctx.DeltaData2);
    }

    [Fact]
    public void SecondEvent_SameData1_ProducesData2Delta()
    {
        var tracker = new DeltaTracker();
        tracker.ComputeAndUpdate(MakeEvent(10, 64));
        // Selber Regler (Data1=10), Data2 ändert sich 64→70 = +6
        var ctx = tracker.ComputeAndUpdate(MakeEvent(10, 70));
        Assert.Equal(6, ctx.DeltaData2);
    }

    [Fact]
    public void SecondEvent_DifferentData1_ProducesDeltaZero()
    {
        var tracker = new DeltaTracker();
        tracker.ComputeAndUpdate(MakeEvent(10, 64));
        // Anderer Regler (Data1=15) → erstes Auftreten dieses Triple-Keys → Delta = 0
        var ctx = tracker.ComputeAndUpdate(MakeEvent(15, 70));
        Assert.Equal(0, ctx.DeltaData2);
    }

    [Fact]
    public void NegativeDelta_CalculatedCorrectly()
    {
        var tracker = new DeltaTracker();
        tracker.ComputeAndUpdate(MakeEvent(10, 64));
        // Selber Regler (Data1=10), Data2 64→60 = -4
        var ctx = tracker.ComputeAndUpdate(MakeEvent(10, 60));
        Assert.Equal(-4, ctx.DeltaData2);
    }

    [Fact]
    public void DD2Positive_IsAbsoluteValue_WhenPositive()
    {
        var tracker = new DeltaTracker();
        tracker.ComputeAndUpdate(MakeEvent(5, 0));
        var ctx = tracker.ComputeAndUpdate(MakeEvent(5, 7));
        Assert.Equal(7, ctx.DD2Positive);
        Assert.Equal(0, ctx.DD2Negative);
    }

    [Fact]
    public void DD2Negative_IsAbsoluteValue_WhenNegative()
    {
        var tracker = new DeltaTracker();
        tracker.ComputeAndUpdate(MakeEvent(5, 10));
        var ctx = tracker.ComputeAndUpdate(MakeEvent(5, 3));
        Assert.Equal(0,  ctx.DD2Positive);
        Assert.Equal(7, ctx.DD2Negative);
    }

    [Fact]
    public void AllDerivedValues_AreZero_WhenDeltaIsZero()
    {
        var tracker = new DeltaTracker();
        tracker.ComputeAndUpdate(MakeEvent(10, 64));
        // Identisches Event: Delta = 0
        var ctx = tracker.ComputeAndUpdate(MakeEvent(10, 64));
        Assert.Equal(0, ctx.DeltaData2);
        Assert.Equal(0, ctx.DD2Positive);
        Assert.Equal(0, ctx.DD2Negative);
    }
}
