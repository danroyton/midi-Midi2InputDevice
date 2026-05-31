using MidiController.Host;
using Microsoft.AspNetCore.Builder;

namespace MidiControllerFrontend.Services;

/// <summary>
/// Startet den ASP.NET Core Kestrel-Backend-Host in-process im selben WPF-Prozess.
/// </summary>
public sealed class BackendHostService : IAsyncDisposable
{
    private WebApplication? _app;
    private readonly CancellationTokenSource _cts = new();

    public async Task StartAsync()
    {
        _app = await BackendStartup.StartAsync(
            configPath: AppContext.BaseDirectory,
            url:        "http://localhost:5000",
            ct:         _cts.Token);
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _cts.CancelAsync();
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
        _cts.Dispose();
    }
}
