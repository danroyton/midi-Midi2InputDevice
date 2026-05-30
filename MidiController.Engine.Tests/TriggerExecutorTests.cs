using MidiController.Domain.Enums;
using MidiController.Domain.Models;
using MidiController.Domain.State;
using MidiController.Engine.Evaluation;
using MidiController.Engine.Execution;
using MidiController.Engine.Pipeline;
using MidiController.Engine.State;
using NSubstitute;
using MidiController.Domain.Interfaces;

namespace MidiController.Engine.Tests;

public class TriggerExecutorTests
{
    private static MidiEvent MakeEvent(int data2 = 64) =>
        new("dev", MidiEventType.ControlChange, 1, 16, data2, 0);

    private static ComputedValueContext MakeCtx(int data2 = 64) =>
        new(MakeEvent(data2), 0, 0);

    private (TriggerExecutor executor, VariableStore store, IInputInjector injector)
        BuildExecutor()
    {
        var store     = new VariableStore();
        store.Set('A', EngineState.GateActive); // aktiviert
        var templates = Substitute.For<ITemplateStore>();
        var injector  = Substitute.For<IInputInjector>();
        var resolver  = new ValueResolver(store);

        var gate      = new GateEvaluator(store);
        var condEval  = new ConditionEvaluator(resolver, templates);
        var actExec   = new ActionExecutor(resolver, injector, store, templates);
        var elseExec  = new ElseExecutor(condEval, actExec, resolver, store);
        var executor  = new TriggerExecutor(gate, condEval, actExec, elseExec, resolver, store);

        return (executor, store, injector);
    }

    private static Trigger MakeTrigger(
        ConditionBlock[]  conditionBlocks,
        ActionBlock[]     actions,
        StateAssignment[]? globalPre  = null,
        StateAssignment[]? globalPost = null,
        TriggerConfig?    elseConfig  = null) =>
        new(
            TriggerId:               "t1",
            DeviceId:                "dev",
            EventType:               MidiEventType.ControlChange,
            Channel:                 1,
            Data1Filter:             16,
            GlobalPreAssignments:    globalPre  ?? [],
            ConditionBlocks:         conditionBlocks,
            Actions:                 actions,
            GlobalPostAssignments:   globalPost ?? [],
            ElseConfig:              elseConfig
        );

    [Fact]
    public async Task Execute_SendsKey_WhenConditionPasses()
    {
        var (executor, _, injector) = BuildExecutor();

        var trigger = MakeTrigger(
            conditionBlocks: [new ConditionBlock(null,
                [new Condition(ValueSource.MidiData2, ">=", ValueSource.Fixed, 10)])],
            actions: [new ActionBlock(null, ["Space"],
                ValueSource.Fixed, 1, ValueSource.Fixed, 0, ValueSource.Fixed, 0, [])]
        );

        await executor.ExecuteAsync(trigger, MakeCtx(data2: 64));

        injector.Received(1).SendKeyCombination(
            Arg.Is<string[]>(k => k[0] == "Space"), 1, 0, 0);
    }

    [Fact]
    public async Task Execute_DoesNotSendKey_WhenConditionFails()
    {
        var (executor, _, injector) = BuildExecutor();

        var trigger = MakeTrigger(
            conditionBlocks: [new ConditionBlock(null,
                [new Condition(ValueSource.MidiData2, ">", ValueSource.Fixed, 100)])],
            actions: [new ActionBlock(null, ["Space"],
                ValueSource.Fixed, 1, ValueSource.Fixed, 0, ValueSource.Fixed, 0, [])]
        );

        await executor.ExecuteAsync(trigger, MakeCtx(data2: 10));

        injector.DidNotReceive().SendKeyCombination(Arg.Any<string[]>(), Arg.Any<int>(),
            Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public async Task Execute_Blocked_WhenGateIsBlocked()
    {
        var (executor, store, injector) = BuildExecutor();
        store.Set('A', EngineState.GateBlocked);

        var trigger = MakeTrigger(
            conditionBlocks: [new ConditionBlock(null,
                [new Condition(ValueSource.Fixed, "==", ValueSource.Fixed, 0)])],
            actions: [new ActionBlock(null, ["Space"],
                ValueSource.Fixed, 1, ValueSource.Fixed, 0, ValueSource.Fixed, 0, [])]
        );

        await executor.ExecuteAsync(trigger, MakeCtx());

        injector.DidNotReceive().SendKeyCombination(Arg.Any<string[]>(), Arg.Any<int>(),
            Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public async Task Execute_AppliesElseConfig_WhenConditionFails()
    {
        var (executor, store, injector) = BuildExecutor();

        var elseConfig = new TriggerConfig(
            ConditionBlocks: [new ConditionBlock(null,
                [new Condition(ValueSource.Fixed, "==", ValueSource.Fixed, 0)])],
            Actions: [new ActionBlock(null, ["Escape"],
                ValueSource.Fixed, 1, ValueSource.Fixed, 0, ValueSource.Fixed, 0, [])],
            GlobalPostAssignments: []
        );

        var trigger = MakeTrigger(
            conditionBlocks: [new ConditionBlock(null,
                [new Condition(ValueSource.MidiData2, ">", ValueSource.Fixed, 100)])],
            actions: [new ActionBlock(null, ["Space"],
                ValueSource.Fixed, 1, ValueSource.Fixed, 0, ValueSource.Fixed, 0, [])],
            elseConfig: elseConfig
        );

        await executor.ExecuteAsync(trigger, MakeCtx(data2: 10));

        injector.Received(1).SendKeyCombination(
            Arg.Is<string[]>(k => k[0] == "Escape"), 1, 0, 0);
    }

    [Fact]
    public async Task Execute_AppliesGlobalPreAssignment_BeforeConditions()
    {
        var (executor, store, _) = BuildExecutor();

        var trigger = MakeTrigger(
            conditionBlocks: [new ConditionBlock(null,
                [new Condition(ValueSource.VariableB, "==", ValueSource.Fixed, 1)])],
            actions: [],
            globalPre: [new StateAssignment('B', ValueSource.Fixed, 1)]
        );

        await executor.ExecuteAsync(trigger, MakeCtx());

        Assert.Equal(1, store.Get('B'));
    }
}
