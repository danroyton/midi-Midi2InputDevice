using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MidiControllerFrontend.Services;
using System.Collections.ObjectModel;

namespace MidiControllerFrontend.ViewModels;

public sealed partial class DevicesViewModel : ObservableObject
{
    private readonly ApiClient _api;

    public ObservableCollection<DeviceInfo> Devices { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    private DeviceInfo? _selectedDevice;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }
    private string _statusMessage = string.Empty;

    public DevicesViewModel(ApiClient api) => _api = api;

    [RelayCommand]
    public async Task RefreshAsync()
    {
        StatusMessage = "Lade…";
        var devices = await _api.ListDevicesAsync();
        Devices.Clear();
        foreach (var d in devices) Devices.Add(d);
        StatusMessage = Devices.Count == 0 ? "Keine Geräte gefunden." : string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        if (SelectedDevice is null) return;
        StatusMessage = $"Verbinde {SelectedDevice.DeviceId}…";
        await _api.ConnectDeviceAsync(SelectedDevice.DeviceId);
        await RefreshAsync();
    }
    private bool CanConnect() => SelectedDevice is { IsConnected: false };

    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    private async Task DisconnectAsync()
    {
        if (SelectedDevice is null) return;
        StatusMessage = $"Trenne {SelectedDevice.DeviceId}…";
        await _api.DisconnectDeviceAsync(SelectedDevice.DeviceId);
        await RefreshAsync();
    }
    private bool CanDisconnect() => SelectedDevice is { IsConnected: true };
}
