using MidiController.Domain.Enums;

namespace MidiController.Domain.Models;

/// <summary>
/// Repräsentiert ein eingehendes MIDI-Ereignis von einem physikalischen Gerät.
/// </summary>
public record MidiEvent(
    string DeviceId,
    MidiEventType Type,
    int Channel,
    int Data1,        // Note-Nummer oder CC-Nummer
    int Data2,        // Velocity oder CC-Wert
    long TimestampUs  // Mikrosekunden seit Prozessstart
);
