using MidiController.Domain.State;
using MidiController.Engine.State;

namespace MidiController.Engine.Tests;

public class VariableStoreTests
{
    [Fact]
    public void InitialValue_OfA_IsGateBlocked()
    {
        var store = new VariableStore();
        Assert.Equal(EngineState.GateBlocked, store.Get('A'));
    }

    [Fact]
    public void InitialValue_OfX_IsOne()
    {
        var store = new VariableStore();
        Assert.Equal(1, store.Get('X'));
    }

    [Fact]
    public void InitialValue_OfUnspecifiedVariable_IsZero()
    {
        var store = new VariableStore();
        Assert.Equal(0, store.Get('B'));
    }

    [Fact]
    public void Set_StoresValue_WhenWithinRange()
    {
        var store = new VariableStore();
        store.Set('B', 42);
        Assert.Equal(42, store.Get('B'));
    }

    [Fact]
    public void Set_ClampsToVarMax_WhenValueTooHigh()
    {
        var store = new VariableStore();
        store.Set('C', 999);
        Assert.Equal(EngineState.VarMax, store.Get('C'));
    }

    [Fact]
    public void Set_ClampsToVarMin_WhenValueTooLow()
    {
        var store = new VariableStore();
        store.Set('C', -999);
        Assert.Equal(EngineState.VarMin, store.Get('C'));
    }

    [Fact]
    public void Reset_RestoresInitialValues()
    {
        var store = new VariableStore();
        store.Set('A', 0);
        store.Set('B', 99);
        store.Reset();
        Assert.Equal(EngineState.GateBlocked, store.Get('A'));
        Assert.Equal(0, store.Get('B'));
    }

    [Fact]
    public void Snapshot_ContainsAllTwentySixVariables()
    {
        var store    = new VariableStore();
        var snapshot = store.Snapshot();
        Assert.Equal(26, snapshot.Count);
    }

    [Fact]
    public void Get_Throws_ForInvalidVariable()
    {
        var store = new VariableStore();
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => store.Get('1')));
    }
}
