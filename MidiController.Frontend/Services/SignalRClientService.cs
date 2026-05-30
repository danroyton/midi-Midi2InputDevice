using Microsoft.AspNetCore.SignalR.Client;

namespace MidiControllerFrontend.Services;

/// <summary>
/// Verwaltet eine persistente SignalR-Verbindung zu einem Hub-Endpunkt.
/// Stellt sicher, dass die Verbindung bei Bedarf wiederhergestellt wird.
/// </summary>
public sealed class SignalRClientService : IAsyncDisposable
{
    private readonly HubConnection _connection;

    public HubConnectionState State => _connection.State;

    public SignalRClientService(string hubUrl)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.Reconnected  += _ => { Reconnected?.Invoke(); return Task.CompletedTask; };
        _connection.Reconnecting += _ => { Reconnecting?.Invoke(); return Task.CompletedTask; };
        _connection.Closed       += _ => { Closed?.Invoke();       return Task.CompletedTask; };
    }

    /// <summary>Verbindet den Client mit dem Hub (idempotent).</summary>
    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_connection.State == HubConnectionState.Disconnected)
            await _connection.StartAsync(ct);
    }

    /// <summary>Trennt die Verbindung.</summary>
    public async Task StopAsync(CancellationToken ct = default)
    {
        if (_connection.State != HubConnectionState.Disconnected)
            await _connection.StopAsync(ct);
    }

    /// <summary>Registriert einen Handler für eine eingehende Nachricht.</summary>
    public IDisposable On<T>(string methodName, Action<T> handler) =>
        _connection.On(methodName, handler);

    /// <summary>Ruft eine Hub-Methode auf.</summary>
    public Task InvokeAsync(string methodName, CancellationToken ct = default) =>
        _connection.InvokeAsync(methodName, ct);

    public Task InvokeAsync<T>(string methodName, T arg, CancellationToken ct = default) =>
        _connection.InvokeAsync(methodName, arg, ct);

    // ── Verbindungsereignisse ─────────────────────────────────────────────────
    public event Action? Reconnected;
    public event Action? Reconnecting;
    public event Action? Closed;

    public async ValueTask DisposeAsync() =>
        await _connection.DisposeAsync();
}
