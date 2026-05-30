using MidiController.Domain.Models;

namespace MidiController.Domain.Interfaces;

/// <summary>
/// Persistiert und lädt wiederverwendbare ConditionBlock- und ActionBlock-Templates.
/// </summary>
public interface ITemplateStore
{
    Task<IEnumerable<string>> ListTemplateNamesAsync(CancellationToken ct = default);
    Task<ConditionBlock?> LoadConditionBlockTemplateAsync(string name, CancellationToken ct = default);
    Task<ActionBlock?> LoadActionBlockTemplateAsync(string name, CancellationToken ct = default);
    Task SaveConditionBlockTemplateAsync(ConditionBlock template, CancellationToken ct = default);
    Task SaveActionBlockTemplateAsync(ActionBlock template, CancellationToken ct = default);
    Task DeleteTemplateAsync(string name, CancellationToken ct = default);
}
