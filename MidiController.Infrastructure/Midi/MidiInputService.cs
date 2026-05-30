using System.Threading.Channels;
using MidiController.Domain.Enums;
using MidiController.Domain.Interfaces;
using MidiController.Domain.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NAudio.Midi;

namespace MidiController.Infrastructure.Midi;

/// <summary>
/// BackgroundService, der physikalische MIDI-Geräte überwacht und eingehende
/// MIDI-Events in einen <see cref="Channel{MidiEvent}"/> schreibt.
/// Reconnect-Logik: Getrennte Geräte werden periodisch neu verbunden.
/// Implementiert außerdem <see cref="IMidiDeviceManager"/>.
/// </summary>
public sealed class MidiInputService : BackgroundService, IMidiDeviceManager
{
    private readonly ChannelWriter<Domain.Models.MidiEvent> _writer;
    private readonly ILogger<MidiInputService> _logger;

    // DeviceId → offener MidiIn
    private readonly Dictionary<string, MidiIn> _openPorts = [];
    private readonly TimeSpan _reconnectInterval = TimeSpan.FromSeconds(5);

    public MidiInputService(
        ChannelWriter<Domain.Models.MidiEvent> writer,
        ILogger<MidiInputService> logger)
    {
        _writer = writer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MidiInputService gestartet.");

        while (!stoppingToken.IsCancellationRequested)
        {
            RefreshDevices();
            try
            {
                await Task.Delay(_reconnectInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        CloseAll();
        _logger.LogInformation("MidiInputService gestoppt.");
    }

    private void RefreshDevices()
    {
        var deviceCount = MidiIn.NumberOfDevices;
        var currentIds  = new HashSet<string>();

        for (int i = 0; i < deviceCount; i++)
        {
            var info     = MidiIn.DeviceInfo(i);
            var deviceId = info.ProductName;
            currentIds.Add(deviceId);

            if (!_openPorts.ContainsKey(deviceId))
                TryOpenDevice(i, deviceId);
        }

        // Geräte schließen, die nicht mehr vorhanden sind
        foreach (var id in _openPorts.Keys.Except(currentIds).ToList())
        {
            _logger.LogWarning("MIDI-Gerät '{DeviceId}' nicht mehr verfügbar – Port wird geschlossen.", id);
            CloseDevice(id);
        }
    }

    private void TryOpenDevice(int index, string deviceId)
    {
        try
        {
            var midi = new MidiIn(index);
            midi.MessageReceived += (_, e) => OnMessage(deviceId, e);
            midi.ErrorReceived   += (_, e) => _logger.LogError(
                "MIDI-Fehler auf '{DeviceId}': {Message}", deviceId, e.MidiEvent);
            midi.Start();
            _openPorts[deviceId] = midi;
            _logger.LogInformation("MIDI-Gerät '{DeviceId}' geöffnet.", deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MIDI-Gerät '{DeviceId}' konnte nicht geöffnet werden.", deviceId);
        }
    }

    private void OnMessage(string deviceId, MidiInMessageEventArgs e)
    {
        var midiEvent = e.MidiEvent;
        var ts        = (long)(e.Timestamp * 1000L); // ms → µs (Näherung)

        Domain.Models.MidiEvent? evt = midiEvent switch
        {
            NoteOnEvent noteOn => new(
                deviceId,
                noteOn.Velocity == 0 ? MidiEventType.NoteOff : MidiEventType.NoteOn,
                noteOn.Channel,
                noteOn.NoteNumber,
                noteOn.Velocity,
                ts),

            NoteEvent noteOff when noteOff.CommandCode == MidiCommandCode.NoteOff => new(
                deviceId,
                MidiEventType.NoteOff,
                noteOff.Channel,
                noteOff.NoteNumber,
                noteOff.Velocity,
                ts),

            ControlChangeEvent cc => new(
                deviceId,
                MidiEventType.ControlChange,
                cc.Channel,
                (int)cc.Controller,
                cc.ControllerValue,
                ts),

            PatchChangeEvent pc => new(
                deviceId,
                MidiEventType.ProgramChange,
                pc.Channel,
                pc.Patch,
                0,
                ts),

            ChannelAfterTouchEvent cat => new(
                deviceId,
                MidiEventType.ChannelPressure,
                cat.Channel,
                cat.AfterTouchPressure,
                0,
                ts),

            PitchWheelChangeEvent pw => new(
                deviceId,
                MidiEventType.PitchBend,
                pw.Channel,
                pw.Pitch,
                0,
                ts),

            _ => null
        };

        if (evt is null)
            return;

        if (!_writer.TryWrite(evt))
            _logger.LogDebug("MIDI-Event-Channel voll – Event verworfen.");
    }

    private void CloseDevice(string deviceId)
    {
        if (!_openPorts.TryGetValue(deviceId, out var midi))
            return;

        try
        {
            midi.Stop();
            midi.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fehler beim Schließen von '{DeviceId}'.", deviceId);
        }
        finally
        {
            _openPorts.Remove(deviceId);
        }
    }

    private void CloseAll()
    {
        foreach (var id in _openPorts.Keys.ToList())
            CloseDevice(id);
    }

    // ── IMidiDeviceManager ────────────────────────────────────────────────────

    public IEnumerable<MidiDeviceInfo> GetPhysicalDevices()
    {
        int count = MidiIn.NumberOfDevices;
        for (int i = 0; i < count; i++)
        {
            var info = MidiIn.DeviceInfo(i);
            yield return new MidiDeviceInfo(info.ProductName, _openPorts.ContainsKey(info.ProductName));
        }
    }

    public IEnumerable<string> GetPhysicalDeviceIds() =>
        GetPhysicalDevices().Select(d => d.DeviceId);

    public Task OpenDeviceAsync(string deviceId, CancellationToken ct = default)
    {
        int count = MidiIn.NumberOfDevices;
        for (int i = 0; i < count; i++)
        {
            if (MidiIn.DeviceInfo(i).ProductName == deviceId && !_openPorts.ContainsKey(deviceId))
            {
                TryOpenDevice(i, deviceId);
                break;
            }
        }
        return Task.CompletedTask;
    }

    public Task CloseDeviceAsync(string deviceId, CancellationToken ct = default)
    {
        CloseDevice(deviceId);
        return Task.CompletedTask;
    }

    // Virtuelle Ports: Stub-Implementierung (erfordert loopMIDI / Windows MIDI Services)
    private readonly List<string> _virtualPorts = [];

    public IEnumerable<string> GetVirtualPortNames() => _virtualPorts.AsReadOnly();

    public Task CreateVirtualPortAsync(string portName, CancellationToken ct = default)
    {
        _logger.LogWarning("CreateVirtualPortAsync: Virtuelle Ports sind noch nicht implementiert.");
        _virtualPorts.Add(portName);
        return Task.CompletedTask;
    }

    public Task DeleteVirtualPortAsync(string portName, CancellationToken ct = default)
    {
        _virtualPorts.Remove(portName);
        return Task.CompletedTask;
    }
}
