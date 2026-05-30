using MidiController.Domain.Models;
using MidiController.Engine.Evaluation;
using MidiController.Engine.Pipeline;
using MidiController.Engine.State;

namespace MidiController.Engine.Execution;

/// <summary>
/// Führt einen einzelnen <see cref="Trigger"/> vollständig aus:
/// GlobalPre → Gate → Prüfblöcke → Aktionen → GlobalPost (bzw. ELSE-Zweig).
/// </summary>
public sealed class TriggerExecutor
{
    private readonly GateEvaluator      _gateEvaluator;
    private readonly ConditionEvaluator _conditionEvaluator;
    private readonly ActionExecutor     _actionExecutor;
    private readonly ElseExecutor       _elseExecutor;
    private readonly ValueResolver      _resolver;
    private readonly VariableStore      _variables;

    public TriggerExecutor(
        GateEvaluator      gateEvaluator,
        ConditionEvaluator conditionEvaluator,
        ActionExecutor     actionExecutor,
        ElseExecutor       elseExecutor,
        ValueResolver      resolver,
        VariableStore      variables)
    {
        _gateEvaluator      = gateEvaluator;
        _conditionEvaluator = conditionEvaluator;
        _actionExecutor     = actionExecutor;
        _elseExecutor       = elseExecutor;
        _resolver           = resolver;
        _variables          = variables;
    }

    /// <summary>
    /// Verarbeitet einen Trigger gegen den aktuellen Event-Kontext.
    /// </summary>
    public async Task ExecuteAsync(
        Trigger              trigger,
        ComputedValueContext  ctx,
        CancellationToken    ct = default)
    {
        var gate = _gateEvaluator.Evaluate();

        if (gate == GateResult.Blocked)
            return;

        ApplyGlobalPreAssignments(trigger.GlobalPreAssignments, ctx);

        if (gate == GateResult.Paused)
        {
            // Im Paused-Zustand: nur Aktionen ausführen, die A direkt setzen
            await ExecutePausedModeActionsAsync(trigger.Actions, ctx, ct);
            return;
        }

        // Normaler Pfad: alle Prüfblöcke (UND-verknüpft) auswerten
        bool allBlocksPassed = await EvaluateAllConditionBlocksAsync(trigger.ConditionBlocks, ctx, ct);

        if (!allBlocksPassed)
        {
            if (trigger.ElseConfig is not null)
                await _elseExecutor.TryExecuteAsync(trigger.ElseConfig, ctx, ct);
            return;
        }

        await ExecuteActionsSequentiallyAsync(trigger.Actions, ctx, ct);
        ApplyGlobalPostAssignments(trigger.GlobalPostAssignments, ctx);
    }

    // ── Hilfsmethoden ────────────────────────────────────────────────────────

    private void ApplyGlobalPreAssignments(
        StateAssignment[]    assignments,
        ComputedValueContext  ctx)
    {
        foreach (var assignment in assignments)
            _variables.Set(assignment.Variable,
                _resolver.Resolve(assignment.Source, assignment.FixedValue, ctx));
    }

    private void ApplyGlobalPostAssignments(
        StateAssignment[]    assignments,
        ComputedValueContext  ctx)
    {
        foreach (var assignment in assignments)
            _variables.Set(assignment.Variable,
                _resolver.Resolve(assignment.Source, assignment.FixedValue, ctx));
    }

    private async Task ExecutePausedModeActionsAsync(
        ActionBlock[]        actions,
        ComputedValueContext  ctx,
        CancellationToken    ct)
    {
        foreach (var action in actions)
        {
            if (ActionSetsGateVariable(action))
                await _actionExecutor.ExecuteAsync(action, ctx, ct);
        }
    }

    private static bool ActionSetsGateVariable(ActionBlock action) =>
        action.StateAssignments.Any(a => a.Variable == 'A');

    private async Task<bool> EvaluateAllConditionBlocksAsync(
        ConditionBlock[]     blocks,
        ComputedValueContext  ctx,
        CancellationToken    ct)
    {
        foreach (var block in blocks)
        {
            if (!await _conditionEvaluator.EvaluateBlockAsync(block, ctx, ct))
                return false;
        }
        return true;
    }

    private async Task ExecuteActionsSequentiallyAsync(
        ActionBlock[]        actions,
        ComputedValueContext  ctx,
        CancellationToken    ct)
    {
        foreach (var action in actions)
            await _actionExecutor.ExecuteAsync(action, ctx, ct);
    }
}
