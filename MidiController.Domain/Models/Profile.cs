namespace MidiController.Domain.Models;

/// <summary>
/// Konfiguration eines physikalischen Geräts und seiner virtuellen Port-Spiegelungen.
/// </summary>
public record DeviceMapping(
    string PhysicalDeviceId,
    string[] VirtualPorts
);

/// <summary>
/// Ein vollständiges Mapping-Profil mit Geräte- und Trigger-Konfigurationen.
/// </summary>
public record Profile(
    string ProfileId,
    DeviceMapping[] Devices,
    Trigger[] Triggers
);
