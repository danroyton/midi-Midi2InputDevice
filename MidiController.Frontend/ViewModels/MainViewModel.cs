using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MidiControllerFrontend.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MidiControllerFrontend.ViewModels;

/// <summary>
/// Haupt-ViewModel: steuert Navigation, Profil-Auswahl und den globalen Status-Indikator.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly DispatcherTimer _pollTimer;

    // ── Externe Navigation (wird von MainWindow gesetzt) ──────────────────────

    public ICommand? NavigateCommand { get; set; }

    // ── Navigation ───────────────────────────────────────────────────────────

    public ObservableCollection<string> NavItems { get; } =
        ["Devices", "Mappings", "Templates", "Log", "Status"];

    public string SelectedNav
    {
        get => _selectedNav;
        set => SetProperty(ref _selectedNav, value);
    }
    private string _selectedNav = "Status";

    // ── Profile ──────────────────────────────────────────────────────────────

    public ObservableCollection<string> Profiles { get; } = new();

    public string? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (SetProperty(ref _selectedProfile, value))
                DeleteProfileCommand.NotifyCanExecuteChanged();
        }
    }
    private string? _selectedProfile;

    // ── Status-Indikator ─────────────────────────────────────────────────────

    /// <summary>0=Aktiv(grün) 1=Pausiert(gelb) 2=Gesperrt(orange) -1=Getrennt(rot)</summary>
    public int GateValue
    {
        get => _gateValue;
        private set
        {
            if (SetProperty(ref _gateValue, value))
                OnPropertyChanged(nameof(IndicatorBrush));
        }
    }
    private int _gateValue = -1;

    public Brush IndicatorBrush => GateValue switch
    {
        0  => Brushes.LimeGreen,
        1  => Brushes.Yellow,
        2  => Brushes.Orange,
        _  => Brushes.Red
    };

    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (SetProperty(ref _isConnected, value) && !value)
                GateValue = -1;
        }
    }
    private bool _isConnected;

    /// <summary>Kurz <c>true</c> wenn ein MIDI-Event eingetroffen ist (für Tray-Blink).</summary>
    public bool MidiActivity
    {
        get => _midiActivity;
        internal set => SetProperty(ref _midiActivity, value);
    }
    private bool _midiActivity;

    /// <summary>Wird vom MidiLogViewModel bei jedem eingehenden MIDI-Event aufgerufen.</summary>
    public void NotifyMidiActivity() => MidiActivity = true;

    // ── Ctor ─────────────────────────────────────────────────────────────────

    public MainViewModel(ApiClient api)
    {
        _api = api;

        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _pollTimer.Tick += async (_, _) => await PollStatusAsync();
        _pollTimer.Start();
    }

    // ── Startup ──────────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        await LoadProfilesAsync();
        await PollStatusAsync();
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ActivateProfileAsync()
    {
        if (SelectedProfile is null) return;
        await _api.ActivateProfileAsync(SelectedProfile);
        await PollStatusAsync();
    }

    [RelayCommand]
    private async Task RefreshProfilesAsync() => await LoadProfilesAsync();

    [RelayCommand]
    private async Task NewProfileAsync()
    {
        var id = $"Profile-{DateTime.Now:yyyyMMdd-HHmmss}";
        await _api.CreateProfileAsync(id);
        await LoadProfilesAsync();
        SelectedProfile = id;
    }

    [RelayCommand(CanExecute = nameof(HasProfileSelection))]
    private async Task DeleteProfileAsync()
    {
        if (SelectedProfile is null) return;
        await _api.DeleteProfileAsync(SelectedProfile);
        await LoadProfilesAsync();
    }

    private bool HasProfileSelection => SelectedProfile is not null;

    [RelayCommand]
    private async Task SetActiveAsync()  => await _api.SetVariableAsync('A', 0);

    [RelayCommand]
    private async Task SetPausedAsync()  => await _api.SetVariableAsync('A', 1);

    [RelayCommand]
    private async Task SetBlockedAsync() => await _api.SetVariableAsync('A', 2);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task LoadProfilesAsync()
    {
        var ids = await _api.ListProfileIdsAsync();
        Profiles.Clear();
        foreach (var id in ids) Profiles.Add(id);
        if (SelectedProfile is null && Profiles.Count > 0)
            SelectedProfile = Profiles[0];
    }

    private async Task PollStatusAsync()
    {
        var status = await _api.GetStatusAsync();
        if (status is null)
        {
            IsConnected = false;
            return;
        }
        IsConnected = true;
        GateValue   = status.GateValue;
    }
}
