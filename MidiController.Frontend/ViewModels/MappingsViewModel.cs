using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MidiControllerFrontend.Services;
using System.Collections.ObjectModel;

namespace MidiControllerFrontend.ViewModels;

/// <summary>
/// Zeigt die Trigger-Liste des aktiven Profils.
/// Vollständiger Trigger-Editor ist ein späterer Ausbauschritt.
/// </summary>
public sealed partial class MappingsViewModel : ObservableObject
{
    private readonly ApiClient _api;

    public string ActiveProfileId
    {
        get => _activeProfileId;
        set => SetProperty(ref _activeProfileId, value);
    }
    private string _activeProfileId = string.Empty;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }
    private string _statusMessage = "Kein Profil geladen.";

    /// <summary>Alle aktiven (verbundenen) MIDI-Geräte für den Gerätefilter.</summary>
    public ObservableCollection<DeviceInfo> ActiveDevices { get; } = new();

    [ObservableProperty]
    private DeviceInfo? _selectedDevice;

    public MappingsViewModel(ApiClient api) => _api = api;

    public async Task LoadForProfileAsync(string profileId)
    {
        ActiveProfileId = profileId;
        StatusMessage   = $"Profil '{profileId}' - Trigger-Editor folgt.";
        await LoadActiveDevicesAsync();
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
}
