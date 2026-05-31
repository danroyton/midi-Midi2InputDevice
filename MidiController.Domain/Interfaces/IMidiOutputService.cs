using MidiController.Domain.Models;

namespace MidiController.Domain.Interfaces;

/// <summary>
/// Sendet MIDI-Nachrichten an physikalische MIDI-Ausgabe-Geräte.
/// </summary>
public interface IMidiOutputService
{
    /// <summary>
    /// Sendet einen einzelnen MIDI-Befehl an das angegebene Gerät.
    /// Gibt <c>false</c> zurück wenn das Gerät nicht gefunden wurde.
    /// </summary>
    bool Send(MidiSendCommand command);
}
