using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MidiController.Domain.Models;

namespace MidiController.Engine;

/// <summary>
/// BackgroundService, der den eingehenden <see cref="Channel{MidiEvent}"/> konsumiert
/// und jedes Event an die <see cref="MappingEngine"/> weitergibt.
/// Läuft auf einem dedizierten Thread mit erhöhter Priorität.
/// </summary>
public sealed class MappingWorker : BackgroundService
{
    private readonly ChannelReader<MidiEvent> _reader;
    private readonly MappingEngine            _mappingEngine;
    private readonly ILogger<MappingWorker>   _logger;

    public MappingWorker(
        ChannelReader<MidiEvent> reader,
        MappingEngine            mappingEngine,
        ILogger<MappingWorker>   logger)
    {
        _reader        = reader;
        _mappingEngine = mappingEngine;
        _logger        = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        SetHighThreadPriority();

        _logger.LogInformation("MappingWorker gestartet.");

        await ConsumeEventsUntilCancelledAsync(stoppingToken);

        _logger.LogInformation("MappingWorker beendet.");
    }

    // ── Hilfsmethoden ────────────────────────────────────────────────────────

    private async Task ConsumeEventsUntilCancelledAsync(CancellationToken ct)
    {
        await foreach (var midiEvent in _reader.ReadAllAsync(ct))
        {
            ProcessEventSafely(midiEvent);
        }
    }

    private void ProcessEventSafely(MidiEvent midiEvent)
    {
        try
        {
            _mappingEngine.ProcessEvent(midiEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Verarbeiten von MidiEvent {DeviceId} {Type}",
                midiEvent.DeviceId, midiEvent.Type);
        }
    }

    private static void SetHighThreadPriority()
    {
        Thread.CurrentThread.Priority = ThreadPriority.Highest;
    }
}
