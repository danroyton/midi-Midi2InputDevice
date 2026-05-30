using MidiController.Domain.Enums;
using MidiController.Domain.Interfaces;
using MidiController.Domain.Models;
using MidiController.Engine.Pipeline;

namespace MidiController.Engine.Evaluation;

/// <summary>
/// Wertet einen <see cref="ConditionBlock"/> aus.
/// Die Bedingungen innerhalb eines Blocks sind ODER-verknüpft:
/// der Block ist wahr, sobald mindestens eine Bedingung erfüllt ist.
/// </summary>
public sealed class ConditionEvaluator
{
    private readonly ValueResolver _resolver;
    private readonly ITemplateStore _templates;

    public ConditionEvaluator(ValueResolver resolver, ITemplateStore templates)
    {
        _resolver  = resolver;
        _templates = templates;
    }

    /// <summary>
    /// Gibt <c>true</c> zurück wenn der Block wahr ist (mind. eine Bedingung erfüllt).
    /// Ist ein TemplateName gesetzt, wird der Block aus dem Store geladen.
    /// </summary>
    public async Task<bool> EvaluateBlockAsync(
        ConditionBlock block,
        ComputedValueContext ctx,
        CancellationToken ct = default)
    {
        var resolvedBlock = await ResolveTemplateIfNeededAsync(block, ct);
        return EvaluateConditionsWithOrLogic(resolvedBlock.Conditions, ctx);
    }

    // ── Hilfsmethoden ────────────────────────────────────────────────────────

    private async Task<ConditionBlock> ResolveTemplateIfNeededAsync(
        ConditionBlock block,
        CancellationToken ct)
    {
        if (block.TemplateName is null)
            return block;

        var template = await _templates.LoadConditionBlockTemplateAsync(block.TemplateName, ct)
            ?? throw new InvalidOperationException(
                $"ConditionBlock-Template '{block.TemplateName}' nicht gefunden.");

        return template;
    }

    private bool EvaluateConditionsWithOrLogic(
        Condition[] conditions,
        ComputedValueContext ctx)
    {
        foreach (var condition in conditions)
        {
            if (EvaluateSingleCondition(condition, ctx))
                return true;
        }
        return false;
    }

    private bool EvaluateSingleCondition(Condition condition, ComputedValueContext ctx)
    {
        int left  = _resolver.Resolve(condition.Left, 0, ctx);
        int right = condition.RightSource == ValueSource.Fixed
            ? condition.RightFixed
            : _resolver.Resolve(condition.RightSource, condition.RightFixed, ctx);

        return condition.Op switch
        {
            "==" => left == right,
            "!=" => left != right,
            "<"  => left <  right,
            ">"  => left >  right,
            "<=" => left <= right,
            ">=" => left >= right,
            _    => throw new InvalidOperationException($"Unbekannter Operator: '{condition.Op}'")
        };
    }
}
