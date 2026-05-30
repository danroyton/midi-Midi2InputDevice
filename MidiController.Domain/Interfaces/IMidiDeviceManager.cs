namespace MidiController.Domain.Interfaces;

/// <summary>
/// Verwaltet physikalische und virtuelle MIDI-Geräte.
/// </summary>
public interface IMidiDeviceManager
{
    /// <summary>Gibt Statusinformationen aller bekannten physikalischen MIDI-Eingabegeräte zurück.</summary>
    IEnumerable<MidiDeviceInfo> GetPhysicalDevices();

    /// <summary>Gibt die IDs aller aktuell verfügbaren physikalischen MIDI-Eingabegeräte zurück.</summary>
    IEnumerable<string> GetPhysicalDeviceIds();

    /// <summary>Öffnet ein einzelnes MIDI-Gerät und beginnt mit dem Empfang von Events.</summary>
    Task OpenDeviceAsync(string deviceId, CancellationToken ct = default);

    /// <summary>Schließt ein einzelnes MIDI-Gerät und stoppt den Event-Empfang.</summary>
    Task CloseDeviceAsync(string deviceId, CancellationToken ct = default);

    /// <summary>Gibt die Namen aller aktuell registrierten virtuellen Ports zurück.</summary>
    IEnumerable<string> GetVirtualPortNames();

    /// <summary>Erstellt einen neuen virtuellen MIDI-Port.</summary>
    Task CreateVirtualPortAsync(string portName, CancellationToken ct = default);

    /// <summary>Entfernt einen virtuellen MIDI-Port.</summary>
    Task DeleteVirtualPortAsync(string portName, CancellationToken ct = default);
}

/// <summary>Statusinformation eines physikalischen MIDI-Eingabegeräts.</summary>
public sealed record MidiDeviceInfo(string DeviceId, bool IsConnected);
