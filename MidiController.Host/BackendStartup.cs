using MidiController.Host.Api;
using MidiController.Host.Extensions;
using MidiController.Host.Hubs;
using MidiController.Host.Signalr;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json.Serialization;

namespace MidiController.Host;

/// <summary>
/// Öffentlicher Einstiegspunkt zum Starten des Backends in einem fremden Prozess (z.B. WPF).
/// Kapselt alle internen Registrierungen und Routing-Konfigurationen.
/// </summary>
public static class BackendStartup
{
    /// <summary>
    /// Erstellt, konfiguriert und startet den Kestrel-Host.
    /// Kehrt zurück sobald der Host bereit ist (Ports gebunden).
    /// </summary>
    /// <param name="configPath">Verzeichnis mit appsettings.backend.json (meist AppContext.BaseDirectory)</param>
    /// <param name="url">Bind-URL, z.B. "http://localhost:5000"</param>
    public static async Task<WebApplication> StartAsync(
        string configPath,
        string url = "http://localhost:5000",
        CancellationToken ct = default)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = configPath,
            EnvironmentName = Environments.Production,
        });

        builder.Configuration
            .AddJsonFile(Path.Combine(configPath, "appsettings.backend.json"), optional: true, reloadOnChange: false);

        builder.WebHost.UseUrls(url);

        // ── Dienste ──────────────────────────────────────────────────────────
        builder.Services
            .AddMidiInfrastructure(builder.Configuration)
            .AddMidiEngine();

        builder.Services.AddSignalR();

        builder.Services.ConfigureHttpJsonOptions(o =>
            o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        builder.Services.AddSingleton<StatusBroadcaster>();
        builder.Services.AddSingleton<MidiEventBroadcaster>();
        builder.Services.AddEndpointsApiExplorer();

        var app = builder.Build();

        // Broadcaster sofort aktivieren
        app.Services.GetRequiredService<StatusBroadcaster>();
        app.Services.GetRequiredService<MidiEventBroadcaster>();

        // ── Routing ──────────────────────────────────────────────────────────
        app.MapHub<StatusHub>("/hubs/status");
        app.MapHub<MidiLogHub>("/hubs/midilog");
        app.MapMidiControllerApi();
        app.MapStatusApi();

        await app.StartAsync(ct);
        return app;
    }
}
