using MidiController.Domain.Enums;
using MidiController.Domain.Interfaces;
using MidiController.Domain.Models;
using MidiController.Domain.State;
using MidiController.Engine.Evaluation;
using MidiController.Engine.Pipeline;
using MidiController.Engine.State;

namespace MidiController.Engine.Execution;

/// <summary>
/// Führt einen einzelnen <see cref="ActionBlock"/> aus:
/// löst X/Y/Z auf, sendet den Tastendruck (falls konfiguriert)
/// und wendet anschließend die State-Zuweisungen an.
/// </summary>
public sealed class ActionExecutor
{
    private readonly ValueResolver  _resolver;
    private readonly IInputInjector _injector;
    private readonly VariableStore  _variables;
    private readonly ITemplateStore _templates;

    public ActionExecutor(
        ValueResolver  resolver,
        IInputInjector injector,
        VariableStore  variables,
        ITemplateStore templates)
    {
        _resolver  = resolver;
        _injector  = injector;
        _variables = variables;
        _templates = templates;
    }

    /// <summary>
    /// Führt einen ActionBlock aus. Template wird bei Bedarf aufgelöst.
    /// </summary>
    public async Task ExecuteAsync(
        ActionBlock          action,
        ComputedValueContext  ctx,
        CancellationToken    ct = default)
    {
        var resolved = await ResolveTemplateIfNeededAsync(action, ct);

        int repeat      = _resolver.Resolve(resolved.XSource, resolved.XFixed, ctx);
        int holdMs      = _resolver.Resolve(resolved.YSource, resolved.YFixed, ctx);
        int pauseMs     = _resolver.Resolve(resolved.ZSource, resolved.ZFixed, ctx);

        if (ShouldSendKeystroke(resolved, repeat))
            _injector.SendKeyCombination(resolved.KeyCombination, repeat, holdMs, pauseMs);

        ApplyStateAssignments(resolved.StateAssignments, ctx);
    }

    // ── Hilfsmethoden ────────────────────────────────────────────────────────

    private async Task<ActionBlock> ResolveTemplateIfNeededAsync(
        ActionBlock action, CancellationToken ct)
    {
        if (action.TemplateName is null)
            return action;

        var template = await _templates.LoadActionBlockTemplateAsync(action.TemplateName, ct)
            ?? throw new InvalidOperationException(
                $"ActionBlock-Template '{action.TemplateName}' nicht gefunden.");

        return template;
    }

    private static bool ShouldSendKeystroke(ActionBlock action, int repeat) =>
        action.KeyCombination.Length > 0 && repeat > 0;

    private void ApplyStateAssignments(
        StateAssignment[]    assignments,
        ComputedValueContext  ctx)
    {
        foreach (var assignment in assignments)
            ApplySingleAssignment(assignment, ctx);
    }

    private void ApplySingleAssignment(StateAssignment assignment, ComputedValueContext ctx)
    {
        ValidateAssignmentTarget(assignment.Variable, assignment.Source);
        int value = _resolver.Resolve(assignment.Source, assignment.FixedValue, ctx);
        _variables.Set(assignment.Variable, value);
    }

    private static void ValidateAssignmentTarget(char variable, ValueSource source)
    {
        if (IsReadOnlyComputedSource(source))
            throw new InvalidOperationException(
                $"DD*-Werte sind schreibgeschützt und dürfen nicht als Ziel einer StateAssignment genutzt werden (Variable '{variable}', Source '{source}').");
    }

    private static bool IsReadOnlyComputedSource(ValueSource source) =>
        source is ValueSource.DD1PosAbs or ValueSource.DD1NegAbs
               or ValueSource.DD1Pos    or ValueSource.DD1Neg
               or ValueSource.DD2PosAbs or ValueSource.DD2NegAbs
               or ValueSource.DD2Pos    or ValueSource.DD2Neg;
}
