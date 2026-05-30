using System.Runtime.Versioning;
using System.Threading.Channels;
using MidiController.Domain.Interfaces;
using MidiController.Domain.Models;
using MidiController.Infrastructure.Config;
using MidiController.Infrastructure.Input;
using MidiController.Infrastructure.Midi;

namespace MidiController.Host.Extensions;

internal static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Registriert Infrastructure-Dienste:
    /// Channel, JsonConfigStore, WindowsInputInjector, MidiInputService.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static IServiceCollection AddMidiInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Konfiguration ────────────────────────────────────────────────────
        var rawPath  = configuration["MidiController:DataPath"] ?? "%APPDATA%\\MidiController";
        var dataPath = Environment.ExpandEnvironmentVariables(rawPath);
        var capacity = configuration.GetValue("MidiController:EventChannelCapacity", 512);

        // ── Channel (verbindet MidiInputService → MappingWorker) ─────────────
        var channel = Channel.CreateBounded<MidiEvent>(new BoundedChannelOptions(capacity)
        {
            FullMode     = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });

        services.AddSingleton(channel.Writer);
        services.AddSingleton(channel.Reader);

        // ── Persistenz ───────────────────────────────────────────────────────
        var configStore = new JsonConfigStore(dataPath);
        services.AddSingleton<IConfigStore>(configStore);
        services.AddSingleton<ITemplateStore>(configStore);

        // ── Input-Injektion ──────────────────────────────────────────────────
        services.AddSingleton<IInputInjector, WindowsInputInjector>();

        // ── MIDI-Eingabe ─────────────────────────────────────────────────────
        services.AddSingleton<MidiInputService>();
        services.AddSingleton<IMidiDeviceManager>(sp => sp.GetRequiredService<MidiInputService>());
        services.AddHostedService(sp => sp.GetRequiredService<MidiInputService>());

        return services;
    }
}
