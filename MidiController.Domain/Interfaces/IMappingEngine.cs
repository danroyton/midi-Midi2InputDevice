using MidiController.Domain.Models;

namespace MidiController.Domain.Interfaces;

/// <summary>
/// Verarbeitet ein eingehendes MIDI-Event gegen alle aktiven Trigger des geladenen Profils.
/// </summary>
public interface IMappingEngine
{
    /// <summary>
    /// Wertet alle passenden Trigger für das Event aus und führt deren Aktionen aus.
    /// </summary>
    void ProcessEvent(MidiEvent midiEvent);
}
