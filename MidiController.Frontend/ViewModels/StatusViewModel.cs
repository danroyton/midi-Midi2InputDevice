using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MidiControllerFrontend.Services;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace MidiControllerFrontend.ViewModels;

/// <summary>
/// ViewModel für den Status-Tab.
/// Hält einen Snapshot aller Variablen A–Z, zeigt Gate-Steuerung und
/// empfängt Echtzeit-Updates über <see cref="SignalRClientService"/> (/hubs/status).
/// </summary>
public sealed partial class StatusViewModel : ObservableObject, IAsyncDisposable
{
    private readonly SignalRClientService _client;
    private readonly ApiClient           _api;
    private readonly Dispatcher          _dispatcher;

    public string ConnectionState
    {
        get => _connectionState;
        private set => SetProperty(ref _connectionState, value);
    }
    private string _connectionState = "Getrennt";

    // ── Gate-Steuerung (Variable A) ──────────────────────────────────────────

    /// <summary>0=Aktiv 1=Pausiert 2=Gesperrt</summary>
    public int GateValue
    {
        get => _gateValue;
        private set
        {
            if (SetProperty(ref _gateValue, value))
            {
                OnPropertyChanged(nameof(GateLabel));
                OnPropertyChanged(nameof(CanActivate));
                OnPropertyChanged(nameof(CanPause));
                OnPropertyChanged(nameof(CanBlock));
                // Commands müssen explizit benachrichtigt werden
                SetActiveCommand.NotifyCanExecuteChanged();
                SetPausedCommand.NotifyCanExecuteChanged();
                SetBlockedCommand.NotifyCanExecuteChanged();
            }
        }
    }
    private int _gateValue = -1;

    public string GateLabel => GateValue switch
    {
        0 => "▢ AKTIV",
        1 => "▤ PAUSIERT",
        2 => "■ GESPERRT",
        _ => "● UNBEKANNT"
    };

    public bool CanActivate => GateValue != 0;
    public bool CanPause    => GateValue != 1;
    public bool CanBlock    => GateValue != 2;

    public ObservableCollection<VariableRow> Variables { get; } = new();

    public StatusViewModel(string hubUrl, ApiClient api)
    {
        _api        = api;
        _dispatcher = Dispatcher.CurrentDispatcher;
        _client     = new SignalRClientService(hubUrl);

        _client.On<VariableSnapshotMessage>("VariableSnapshot", ApplySnapshot);
        _client.On<VariableChangedMessage>("VariableChanged",   ApplyChange);

        _client.Reconnected  += () => ConnectionState = "Verbunden";
        _client.Reconnecting += () => ConnectionState = "Verbindet…";
        _client.Closed       += () => ConnectionState = "Getrennt";
    }

    /// <summary>Wird von der View beim Laden aufgerufen, um automatisch zu verbinden.</summary>
    public async Task AutoConnectAsync()
    {
        if (_client.State == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Disconnected)
            await ConnectAsync();
    }

    [RelayCommand]
    public async Task ConnectAsync()
    {
        ConnectionState = "Verbindet…";
        try
        {
            await _client.StartAsync();
            ConnectionState = "Verbunden";
        }
        catch (Exception ex)
        {
            ConnectionState = $"Fehler: {ex.Message}";
        }
    }

    // ── Gate-Befehle ─────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanActivate))]
    private async Task SetActiveAsync()  => await SetGateAsync(0);

    [RelayCommand(CanExecute = nameof(CanPause))]
    private async Task SetPausedAsync()  => await SetGateAsync(1);

    [RelayCommand(CanExecute = nameof(CanBlock))]
    private async Task SetBlockedAsync() => await SetGateAsync(2);

    private async Task SetGateAsync(int value) =>
        await _api.SetVariableAsync('A', value);

    // ── Nachrichten-Handler ───────────────────────────────────────────────────

    private void ApplySnapshot(VariableSnapshotMessage msg)
    {
        _dispatcher.Invoke(() =>
        {
            Variables.Clear();
            foreach (var (variable, value) in msg.Variables.OrderBy(x => x.Key))
            {
                var alias = msg.Aliases.TryGetValue(variable, out var a) ? a : variable.ToString();
                Variables.Add(new VariableRow(variable, alias, value));
            }
            // Gate aus Snapshot initialisieren
            if (msg.Variables.TryGetValue('A', out var gateVal))
                GateValue = gateVal;
        });
    }

    private void ApplyChange(VariableChangedMessage msg)
    {
        _dispatcher.Invoke(() =>
        {
            var row = Variables.FirstOrDefault(r => r.Variable == msg.Variable);
            if (row is not null)
            {
                var idx = Variables.IndexOf(row);
                Variables[idx] = row with { Value = msg.Value };
            }
            if (msg.Variable == 'A')
                GateValue = msg.Value;
        });
    }

    public async ValueTask DisposeAsync() =>
        await _client.DisposeAsync();
}

/// <summary>Repräsentiert eine einzelne Zustandsvariable in der UI-Tabelle.</summary>
public record VariableRow(char Variable, string Alias, int Value);
