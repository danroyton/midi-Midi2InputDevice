using MidiController.Host.Api;
using MidiController.Host.Extensions;
using MidiController.Host.Hubs;
using MidiController.Host.Signalr;

var builder = WebApplication.CreateBuilder(args);

// ── Dienste registrieren ─────────────────────────────────────────────────────
builder.Services
    .AddMidiInfrastructure(builder.Configuration)
    .AddMidiEngine();

// SignalR
builder.Services.AddSignalR();

// Broadcaster als Singletons – Konstruktor abonniert Events automatisch
builder.Services.AddSingleton<StatusBroadcaster>();
builder.Services.AddSingleton<MidiEventBroadcaster>();

// Minimal-API-Metadaten
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Broadcaster sofort instanziieren, damit Events ab Start empfangen werden
app.Services.GetRequiredService<StatusBroadcaster>();
app.Services.GetRequiredService<MidiEventBroadcaster>();

// ── SignalR-Hubs ─────────────────────────────────────────────────────────────
app.MapHub<StatusHub>("/hubs/status");
app.MapHub<MidiLogHub>("/hubs/midilog");

// ── REST-Endpunkte ───────────────────────────────────────────────────────────
app.MapMidiControllerApi();
app.MapStatusApi();

app.Run();

