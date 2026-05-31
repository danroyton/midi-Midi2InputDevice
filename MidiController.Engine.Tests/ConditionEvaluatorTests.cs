using MidiController.Domain.Enums;
using MidiController.Domain.Models;
using MidiController.Engine.Evaluation;
using MidiController.Engine.Pipeline;
using MidiController.Engine.State;
using NSubstitute;

namespace MidiController.Engine.Tests;

public class ConditionEvaluatorTests
{
    private static ComputedValueContext MakeCtx(int data2 = 50) =>
        new(new MidiEvent("dev", MidiEventType.ControlChange, 1, 0, data2, 0), 0);

    private static ConditionEvaluator MakeEvaluator(VariableStore? store = null)
    {
        store ??= new VariableStore();
        var resolver  = new ValueResolver(store);
        var templates = Substitute.For<MidiController.Domain.Interfaces.ITemplateStore>();
        return new ConditionEvaluator(resolver, templates);
    }

    [Fact]
    public async Task Block_IsTrue_WhenAtLeastOneConditionMatches()
    {
        var block = new ConditionBlock(null,
        [
            new Condition(ValueSource.MidiData2, ">=", ValueSource.Fixed, 10),
            new Condition(ValueSource.MidiData2, "==", ValueSource.Fixed, 99),
        ]);

        var evaluator = MakeEvaluator();
        Assert.True(await evaluator.EvaluateBlockAsync(block, MakeCtx(data2: 50)));
    }

    [Fact]
    public async Task Block_IsFalse_WhenNoConditionMatches()
    {
        var block = new ConditionBlock(null,
        [
            new Condition(ValueSource.MidiData2, ">", ValueSource.Fixed, 100),
        ]);

        var evaluator = MakeEvaluator();
        Assert.False(await evaluator.EvaluateBlockAsync(block, MakeCtx(data2: 50)));
    }

    [Fact]
    public async Task Block_ReadesVariable_FromStore()
    {
        var store = new VariableStore();
        store.Set('B', 1);

        var block = new ConditionBlock(null,
        [
            new Condition(ValueSource.VariableB, "==", ValueSource.Fixed, 1),
        ]);

        var evaluator = MakeEvaluator(store);
        Assert.True(await evaluator.EvaluateBlockAsync(block, MakeCtx()));
    }

    [Theory]
    [InlineData("==", 5, 5,  true)]
    [InlineData("!=", 5, 6,  true)]
    [InlineData("<",  4, 5,  true)]
    [InlineData(">",  6, 5,  true)]
    [InlineData("<=", 5, 5,  true)]
    [InlineData(">=", 5, 5,  true)]
    [InlineData("==", 5, 6,  false)]
    public async Task AllOperators_EvaluateCorrectly(string op, int leftData2, int rightFixed, bool expected)
    {
        var block = new ConditionBlock(null,
        [
            new Condition(ValueSource.MidiData2, op, ValueSource.Fixed, rightFixed),
        ]);

        var evaluator = MakeEvaluator();
        Assert.Equal(expected, await evaluator.EvaluateBlockAsync(block, MakeCtx(data2: leftData2)));
    }
}
