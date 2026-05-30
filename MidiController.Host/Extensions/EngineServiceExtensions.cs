using MidiController.Engine;
using MidiController.Engine.Evaluation;
using MidiController.Engine.Execution;
using MidiController.Engine.Pipeline;
using MidiController.Engine.State;

namespace MidiController.Host.Extensions;

internal static class EngineServiceExtensions
{
    /// <summary>
    /// Registriert alle Engine-Klassen als Singletons und den MappingWorker als HostedService.
    /// </summary>
    internal static IServiceCollection AddMidiEngine(this IServiceCollection services)
    {
        // Zustand
        services.AddSingleton<VariableStore>();
        services.AddSingleton<DeltaTracker>();

        // Auswertung
        services.AddSingleton<ValueResolver>();
        services.AddSingleton<GateEvaluator>();
        services.AddSingleton<ConditionEvaluator>();

        // Ausführung
        services.AddSingleton<ActionExecutor>();
        services.AddSingleton<ElseExecutor>();
        services.AddSingleton<TriggerExecutor>();

        // Engine-Einstiegspunkt
        services.AddSingleton<MappingEngine>();

        // BackgroundService
        services.AddHostedService<MappingWorker>();

        return services;
    }
}
