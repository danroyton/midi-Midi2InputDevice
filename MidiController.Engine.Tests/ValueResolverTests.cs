using MidiController.Domain.Enums;
using MidiController.Domain.Models;
using MidiController.Engine.Evaluation;
using MidiController.Engine.Pipeline;
using MidiController.Engine.State;

namespace MidiController.Engine.Tests;

public class ValueResolverTests
{
    private static ComputedValueContext MakeCtx(int data1 = 10, int data2 = 64,
        int deltaData2 = -2) =>
        new(new MidiEvent("dev", MidiEventType.ControlChange, 1, data1, data2, 0),
            deltaData2);

    [Fact]
    public void Resolve_Fixed_ReturnsFixedValue()
    {
        var resolver = new ValueResolver(new VariableStore());
        Assert.Equal(42, resolver.Resolve(ValueSource.Fixed, 42, MakeCtx()));
    }

    [Fact]
    public void Resolve_MidiData1_ReturnsData1()
    {
        var resolver = new ValueResolver(new VariableStore());
        Assert.Equal(10, resolver.Resolve(ValueSource.MidiData1, 0, MakeCtx(data1: 10)));
    }

    [Fact]
    public void Resolve_DeltaData2_ReturnsDelta()
    {
        var resolver = new ValueResolver(new VariableStore());
        Assert.Equal(5, resolver.Resolve(ValueSource.DeltaData2, 0, MakeCtx(deltaData2: 5)));
    }

    [Fact]
    public void Resolve_DD2Positive_ReturnsAbsPositiveDelta()
    {
        var resolver = new ValueResolver(new VariableStore());
        Assert.Equal(3, resolver.Resolve(ValueSource.DD2Positive, 0, MakeCtx(deltaData2: 3)));
    }

    [Fact]
    public void Resolve_DD2Positive_ReturnsZero_WhenDeltaNegative()
    {
        var resolver = new ValueResolver(new VariableStore());
        Assert.Equal(0, resolver.Resolve(ValueSource.DD2Positive, 0, MakeCtx(deltaData2: -3)));
    }

    [Fact]
    public void Resolve_DD2Negative_ReturnsAbsNegativeDelta()
    {
        var resolver = new ValueResolver(new VariableStore());
        Assert.Equal(3, resolver.Resolve(ValueSource.DD2Negative, 0, MakeCtx(deltaData2: -3)));
    }

    [Fact]
    public void Resolve_DD2Negative_ReturnsZero_WhenDeltaPositive()
    {
        var resolver = new ValueResolver(new VariableStore());
        Assert.Equal(0, resolver.Resolve(ValueSource.DD2Negative, 0, MakeCtx(deltaData2: 3)));
    }

    [Fact]
    public void Resolve_VariableB_ReturnsStoredValue()
    {
        var store = new VariableStore();
        store.Set('B', 55);
        var resolver = new ValueResolver(store);
        Assert.Equal(55, resolver.Resolve(ValueSource.VariableB, 0, MakeCtx()));
    }

    [Fact]
    public void Resolve_UnknownSource_Throws()
    {
        var resolver = new ValueResolver(new VariableStore());
        Assert.Throws<ArgumentOutOfRangeException>(
            () => resolver.Resolve((ValueSource)9999, 0, MakeCtx()));
    }
}
