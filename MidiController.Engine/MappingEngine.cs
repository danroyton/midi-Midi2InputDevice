using MidiController.Domain.Enums;
using MidiController.Domain.Interfaces;
using MidiController.Domain.Models;
using MidiController.Engine.Execution;
using MidiController.Engine.Pipeline;

namespace MidiController.Engine;

/// <summary>
/// Einstiegspunkt der Mapping-Logik. Empfängt ein <see cref="MidiEvent"/>,
/// berechnet den Kontext und führt alle passenden Trigger des aktiven Profils aus.
/// </summary>
public sealed class MappingEngine : IMappingEngine
{
    private readonly DeltaTracker    _deltaTracker;
    private readonly TriggerExecutor _triggerExecutor;

    private Profile? _activeProfile;

    /// <summary>
    /// Wird für jedes eingehende MIDI-Event gefeuert, bevor Trigger ausgeführt werden.
    /// Wird vom MidiEventBroadcaster für den Live-Log genutzt.
    /// </summary>
    public event Action<MidiEvent>? MidiEventReceived;

    public MappingEngine(DeltaTracker deltaTracker, TriggerExecutor triggerExecutor)
    {
        _deltaTracker    = deltaTracker;
        _triggerExecutor = triggerExecutor;
    }

    /// <summary>Lädt ein Profil als aktives Profil.</summary>
    public void LoadProfile(Profile profile)
    {
        _activeProfile = profile;
    }

    /// <inheritdoc/>
    public void ProcessEvent(MidiEvent midiEvent)
    {
        MidiEventReceived?.Invoke(midiEvent);

        if (_activeProfile is null)
            return;

        var ctx = _deltaTracker.ComputeAndUpdate(midiEvent);

        var matchingTriggers = FindMatchingTriggers(_activeProfile.Triggers, midiEvent, ctx);

        foreach (var trigger in matchingTriggers)
            _ = _triggerExecutor.ExecuteAsync(trigger, ctx);
    }

    // ── Hilfsmethoden ────────────────────────────────────────────────────────

    private static IEnumerable<Trigger> FindMatchingTriggers(
        Trigger[]           triggers,
        MidiEvent           midiEvent,
        ComputedValueContext ctx) =>
        triggers.Where(t => TriggerMatchesEvent(t, midiEvent, ctx));

    private static bool TriggerMatchesEvent(
        Trigger             trigger,
        MidiEvent           midiEvent,
        ComputedValueContext ctx)
    {
        // Channel, EventType, Data1 immer exakt vergleichen
        if (trigger.DeviceId  != midiEvent.DeviceId) return false;
        if (trigger.EventType != midiEvent.Type)     return false;
        if (trigger.Channel   != midiEvent.Channel)  return false;
        if (trigger.Data1Filter is not null && trigger.Data1Filter != midiEvent.Data1) return false;

        // Vierter Parameter: Data2 / Delta / Variable
        return trigger.MatchMode switch
        {
            TriggerMatchMode.Variable    => true,
            TriggerMatchMode.Data2       => midiEvent.Data2    == trigger.MatchValue,
            TriggerMatchMode.DeltaData2  => ctx.DeltaData2     == trigger.MatchValue,
            TriggerMatchMode.DD2Positive => ctx.DD2Positive    == trigger.MatchValue,
            TriggerMatchMode.DD2Negative => ctx.DD2Negative    == trigger.MatchValue,
            _                            => false,
        };
    }
}
