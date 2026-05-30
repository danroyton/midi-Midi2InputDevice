using MidiController.Domain.Models;
using MidiController.Engine.Evaluation;
using MidiController.Engine.Pipeline;
using MidiController.Engine.State;

namespace MidiController.Engine.Execution;

/// <summary>
/// Führt den ELSE-Zweig eines Triggers aus, wenn ein Prüfblock fehlschlägt.
/// Hat dieselbe Struktur wie ein normaler Trigger (Prüfblöcke + Aktionen + GlobalPost).
/// </summary>
public sealed class ElseExecutor
{
    private readonly ConditionEvaluator _conditionEvaluator;
    private readonly ActionExecutor     _actionExecutor;
    private readonly ValueResolver      _resolver;
    private readonly VariableStore      _variables;

    public ElseExecutor(
        ConditionEvaluator conditionEvaluator,
        ActionExecutor     actionExecutor,
        ValueResolver      resolver,
        VariableStore      variables)
    {
        _conditionEvaluator = conditionEvaluator;
        _actionExecutor     = actionExecutor;
        _resolver           = resolver;
        _variables          = variables;
    }

    /// <summary>
    /// Wertet den ELSE-Zweig aus. Gibt <c>true</c> zurück wenn alle Prüfblöcke bestanden
    /// und die Aktionen ausgeführt wurden.
    /// </summary>
    public async Task<bool> TryExecuteAsync(
        TriggerConfig        elseConfig,
        ComputedValueContext  ctx,
        CancellationToken    ct = default)
    {
        bool allBlocksPassed = await EvaluateAllConditionBlocksAsync(elseConfig.ConditionBlocks, ctx, ct);
        if (!allBlocksPassed)
            return false;

        await ExecuteActionsSequentiallyAsync(elseConfig.Actions, ctx, ct);
        ApplyGlobalPostAssignments(elseConfig.GlobalPostAssignments, ctx);
        return true;
    }

    // ── Hilfsmethoden ────────────────────────────────────────────────────────

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

    private void ApplyGlobalPostAssignments(
        StateAssignment[]    assignments,
        ComputedValueContext  ctx)
    {
        foreach (var assignment in assignments)
            _variables.Set(assignment.Variable,
                _resolver.Resolve(assignment.Source, assignment.FixedValue, ctx));
    }
}
