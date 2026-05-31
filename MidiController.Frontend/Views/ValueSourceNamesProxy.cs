using MidiControllerFrontend.ViewModels;

namespace MidiControllerFrontend.Views;

/// <summary>
/// Statischer Proxy damit XAML auf ValueSourceNames zugreifen kann ({x:Static}).
/// </summary>
public static class ValueSourceNamesProxy
{
    public static IReadOnlyList<string> WritableSources => ValueSourceNames.WritableSources;
    public static IReadOnlyList<string> Variables       => ValueSourceNames.Variables;
    public static IReadOnlyList<string> ConditionSources => ValueSourceNames.ConditionSources;
    public static IReadOnlyList<string> CompareOps      => ValueSourceNames.CompareOps;
    public static IReadOnlyList<string> MidiEventTypes  => ValueSourceNames.MidiEventTypes;
}
