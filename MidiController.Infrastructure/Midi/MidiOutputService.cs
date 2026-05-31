using MidiController.Domain.Interfaces;
using MidiController.Domain.Models;
using Microsoft.Extensions.Logging;
using NAudio.Midi;

namespace MidiController.Infrastructure.Midi;

/// <summary>
/// Sendet MIDI-Nachrichten an physikalische MIDI-Ausgabe-Geräte via NAudio MidiOut.
/// Geöffnete Ports werden gecacht und beim Dispose geschlossen.
/// </summary>
public sealed class MidiOutputService : IMidiOutputService, IDisposable
{
    private readonly ILogger<MidiOutputService> _logger;
    private readonly Dictionary<string, MidiOut> _openPorts = [];

    public MidiOutputService(ILogger<MidiOutputService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool Send(MidiSendCommand command)
    {
        try
        {
            var port = GetOrOpen(command.DeviceId);
            if (port is null)
                return false;

            int message = BuildRawMessage(command);
            port.Send(message);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MIDI-Send fehlgeschlagen an Gerät '{DeviceId}'.", command.DeviceId);
            return false;
        }
    }

    // ── Hilfsmethoden ────────────────────────────────────────────────────────

    private MidiOut? GetOrOpen(string deviceId)
    {
        if (_openPorts.TryGetValue(deviceId, out var existing))
            return existing;

        int deviceIndex = FindDeviceIndex(deviceId);
        if (deviceIndex < 0)
        {
            _logger.LogWarning("MIDI-Ausgabegerät '{DeviceId}' nicht gefunden.", deviceId);
            return null;
        }

        var port = new MidiOut(deviceIndex);
        _openPorts[deviceId] = port;
        _logger.LogInformation("MIDI-Ausgabegerät '{DeviceId}' geöffnet (Index {Index}).", deviceId, deviceIndex);
        return port;
    }

    private static int FindDeviceIndex(string deviceId)
    {
        int count = MidiOut.NumberOfDevices;
        for (int i = 0; i < count; i++)
        {
            if (MidiOut.DeviceInfo(i).ProductName.Equals(deviceId, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Baut eine rohe 3-Byte-MIDI-Nachricht als int zusammen.
    /// </summary>
    private static int BuildRawMessage(MidiSendCommand cmd)
    {
        // Status-Byte: obere 4 Bits = Event-Typ, untere 4 Bits = Kanal-1
        int channel = Math.Clamp(cmd.Channel - 1, 0, 15);
        int statusNibble = cmd.EventType switch
        {
            "NoteOff"       => 0x80,
            "NoteOn"        => 0x90,
            "ControlChange" => 0xB0,
            "ProgramChange" => 0xC0,
            "PitchBend"     => 0xE0,
            _               => 0x90,
        };

        int status = statusNibble | channel;
        int data1  = Math.Clamp(cmd.Data1, 0, 127);
        int data2  = Math.Clamp(cmd.Data2, 0, 127);

        return status | (data1 << 8) | (data2 << 16);
    }

    public void Dispose()
    {
        foreach (var (_, port) in _openPorts)
            port.Dispose();
        _openPorts.Clear();
    }
}
