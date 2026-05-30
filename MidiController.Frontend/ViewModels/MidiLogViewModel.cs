using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MidiControllerFrontend.Services;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace MidiControllerFrontend.ViewModels;

/// <summary>
/// ViewModel für den MIDI-Log-Tab.
/// Streamt eingehende MIDI-Events über <see cref="SignalRClientService"/> (/hubs/midilog).
/// </summary>
public sealed partial class MidiLogViewModel : ObservableObject, IAsyncDisposable
{
    private const int MaxEntries = 1000;

    private readonly SignalRClientService _client;
    private readonly ApiClient           _api;
    private readonly Dispatcher          _dispatcher;

    public string ConnectionState
    {
        get => _connectionState;
        private set => SetProperty(ref _connectionState, value);
    }
    private string _connectionState = "Getrennt";

    /// <summary>Alle aktiven (verbundenen) MIDI-Geräte für den Gerätefilter.</summary>
    public ObservableCollection<DeviceInfo> ActiveDevices { get; } = new();

    [ObservableProperty]
    private DeviceInfo? _selectedDevice;

    /// <summary>Interner Filterwert, abgeleitet aus <see cref="SelectedDevice"/>.</summary>
    private string DeviceFilter => SelectedDevice?.DeviceId ?? string.Empty;

    public ObservableCollection<MidiLogEntry> Entries { get; } = new();

    public MidiLogViewModel(string hubUrl, ApiClient api)
    {
        _api        = api;
        _dispatcher = Dispatcher.CurrentDispatcher;
        _client     = new SignalRClientService(hubUrl);

        _client.On<MidiLogMessage>("MidiEventReceived", OnMidiEvent);

        _client.Reconnected  += () => ConnectionState = "Verbunden";
        _client.Reconnecting += () => ConnectionState = "Verbindet…";
        _client.Closed       += () => ConnectionState = "Getrennt";
    }

    /// <summary>Wird von der View beim Laden aufgerufen, um automatisch zu verbinden.</summary>
    public async Task AutoConnectAsync()
    {
        await LoadActiveDevicesAsync();
        if (_client.State == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Disconnected)
            await ConnectAsync();
    }

    [RelayCommand]
    public async Task LoadActiveDevicesAsync()
    {
        var devices = await _api.ListDevicesAsync();
        ActiveDevices.Clear();
        foreach (var d in devices.Where(d => d.IsConnected))
            ActiveDevices.Add(d);

        if (SelectedDevice is not null && !ActiveDevices.Contains(SelectedDevice))
            SelectedDevice = null;
    }

    [RelayCommand]
    public async Task ConnectAsync()
    {
        ConnectionState = "Verbindet…";
        try
        {
            await _client.StartAsync();
            ConnectionState = "Verbunden";

            // Gerätefilter setzen falls vorhanden
            if (!string.IsNullOrWhiteSpace(DeviceFilter))
                await _client.InvokeAsync("SubscribeDevice", DeviceFilter);
        }
        catch (Exception ex)
        {
            ConnectionState = $"Fehler: {ex.Message}";
        }
    }

    [RelayCommand]
    public void ClearLog() => _dispatcher.Invoke(Entries.Clear);

    // ── Nachrichten-Handler ───────────────────────────────────────────────────

    private void OnMidiEvent(MidiLogMessage msg)
    {
        _dispatcher.Invoke(() =>
        {
            if (Entries.Count >= MaxEntries)
                Entries.RemoveAt(0);

            Entries.Add(new MidiLogEntry(
                Timestamp: TimeSpan.FromMicroseconds(msg.TimestampUs).ToString(@"hh\:mm\:ss\.fff"),
                DeviceId:  msg.DeviceId,
                EventType: msg.EventType,
                Channel:   msg.Channel,
                Data1:     msg.Data1,
                Data2:     msg.Data2));
        });
    }

    public async ValueTask DisposeAsync() =>
        await _client.DisposeAsync();
}

/// <summary>Eine Zeile im MIDI-Log.</summary>
public record MidiLogEntry(
    string Timestamp,
    string DeviceId,
    string EventType,
    int    Channel,
    int    Data1,
    int    Data2);
