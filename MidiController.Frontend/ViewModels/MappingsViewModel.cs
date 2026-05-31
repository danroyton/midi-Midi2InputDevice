using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MidiControllerFrontend.Services;
using System.Collections.ObjectModel;

namespace MidiControllerFrontend.ViewModels;

/// <summary>
/// Bearbeitungsmodell für eine einzelne Aktion (Tastenkombination + Wiederholung/Dauer/Pause).
/// </summary>
public sealed partial class ActionEditModel : ObservableObject
{
    /// <summary>Komma-getrennte Tastenbezeichnungen, z.B. "CTRL,C".</summary>
    [ObservableProperty] private string _keys        = string.Empty;
    /// <summary>Anzahl Wiederholungen (X), fester Wert.</summary>
    [ObservableProperty] private int    _repeatCount = 1;
    /// <summary>Tastendruck-Dauer in ms (Y).</summary>
    [ObservableProperty] private int    _durationMs  = 50;
    /// <summary>Pause nach Tastendruck in ms (Z).</summary>
    [ObservableProperty] private int    _pauseMs     = 0;

    public string Summary =>
        $"{(string.IsNullOrWhiteSpace(Keys) ? "(keine Taste)" : Keys)}  ×{RepeatCount}  {DurationMs}ms/{PauseMs}ms";

    partial void OnKeysChanged(string v)        => OnPropertyChanged(nameof(Summary));
    partial void OnRepeatCountChanged(int v)    => OnPropertyChanged(nameof(Summary));
    partial void OnDurationMsChanged(int v)     => OnPropertyChanged(nameof(Summary));
    partial void OnPauseMsChanged(int v)        => OnPropertyChanged(nameof(Summary));

    public ActionBlockDto ToDto() => new(
        TemplateName:     null,
        KeyCombination:   Keys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        XSource:          "Fixed", XFixed: RepeatCount,
        YSource:          "Fixed", YFixed: DurationMs,
        ZSource:          "Fixed", ZFixed: PauseMs,
        StateAssignments: []
    );

    public static ActionEditModel FromDto(ActionBlockDto dto) => new()
    {
        Keys        = string.Join(", ", dto.KeyCombination),
        RepeatCount = dto.XSource == "Fixed" ? dto.XFixed : 1,
        DurationMs  = dto.YSource == "Fixed" ? dto.YFixed : 50,
        PauseMs     = dto.ZSource == "Fixed" ? dto.ZFixed : 0,
    };
}

public sealed partial class TriggerEditModel : ObservableObject
{
    [ObservableProperty] private string _triggerId   = Guid.NewGuid().ToString("N")[..8];
    [ObservableProperty] private string _deviceId    = string.Empty;
    [ObservableProperty] private string _eventType   = "ControlChange";
    [ObservableProperty] private int    _channel     = 1;
    [ObservableProperty] private int?   _data1Filter;
    [ObservableProperty] private string _matchMode   = "Variable";
    [ObservableProperty] private int    _matchValue;
    [ObservableProperty] private bool   _matchValueVisible;
    [ObservableProperty] private ActionEditModel? _selectedAction;

    public ObservableCollection<ActionEditModel> Actions { get; } = new();

    public string Summary =>
        $"{DeviceId}  Ch{Channel}  {EventType}" +
        (Data1Filter.HasValue ? $"  #{Data1Filter}" : "") +
        $"  [{MatchMode}{(MatchMode != "Variable" ? $"={MatchValue}" : "")}]";

    partial void OnMatchModeChanged(string value)
    {
        MatchValueVisible = value != "Variable";
        OnPropertyChanged(nameof(Summary));
    }
    partial void OnDeviceIdChanged(string value)  => OnPropertyChanged(nameof(Summary));
    partial void OnEventTypeChanged(string value) => OnPropertyChanged(nameof(Summary));
    partial void OnChannelChanged(int value)      => OnPropertyChanged(nameof(Summary));
    partial void OnData1FilterChanged(int? value) => OnPropertyChanged(nameof(Summary));
    partial void OnMatchValueChanged(int value)   => OnPropertyChanged(nameof(Summary));

    [RelayCommand]
    private void AddAction()
    {
        var a = new ActionEditModel();
        Actions.Add(a);
        SelectedAction = a;
    }

    [RelayCommand]
    private void RemoveAction(ActionEditModel action) => Actions.Remove(action);

    public TriggerDto ToDto() => new(
        TriggerId:             TriggerId,
        DeviceId:              DeviceId,
        EventType:             EventType,
        Channel:               Channel,
        Data1Filter:           Data1Filter,
        MatchMode:             MatchMode,
        MatchValue:            MatchValue,
        GlobalPreAssignments:  [],
        ConditionBlocks:       [],
        Actions:               [.. Actions.Select(a => a.ToDto())],
        GlobalPostAssignments: [],
        ElseConfig:            null
    );

    public static TriggerEditModel FromDto(TriggerDto dto)
    {
        var m = new TriggerEditModel
        {
            TriggerId         = dto.TriggerId,
            DeviceId          = dto.DeviceId,
            EventType         = dto.EventType,
            Channel           = dto.Channel,
            Data1Filter       = dto.Data1Filter,
            MatchMode         = dto.MatchMode,
            MatchValue        = dto.MatchValue,
            MatchValueVisible = dto.MatchMode != "Variable",
        };
        foreach (var a in dto.Actions)
            m.Actions.Add(ActionEditModel.FromDto(a));
        return m;
    }
}

public sealed partial class MappingsViewModel : ObservableObject
{
    private readonly ApiClient            _api;
    private readonly SignalRClientService _logHub;
    private IDisposable? _midiSubscription;

    [ObservableProperty] private string _activeProfileId = string.Empty;
    [ObservableProperty] private string _statusMessage   = "Kein Profil geladen.";

    public ObservableCollection<DeviceInfo>      ActiveDevices { get; } = new();
    [ObservableProperty] private DeviceInfo?     _selectedDevice;
    public ObservableCollection<TriggerEditModel> Triggers { get; } = new();
    [ObservableProperty] private TriggerEditModel? _selectedTrigger;
    [ObservableProperty] private bool              _isEditorOpen;
    [ObservableProperty] private TriggerEditModel  _editor = new();
    [ObservableProperty] private bool              _isCapturing;
    [ObservableProperty] private string            _captureStatus = string.Empty;

    public IReadOnlyList<string> EventTypes { get; } =
        ["NoteOn", "NoteOff", "ControlChange", "ProgramChange", "PitchBend", "ChannelAftertouch"];
    public IReadOnlyList<string> MatchModes { get; } =
        ["Variable", "Data2", "DeltaData2", "DD2Positive", "DD2Negative"];

    public MappingsViewModel(ApiClient api, SignalRClientService logHub)
    {
        _api    = api;
        _logHub = logHub;
    }

    public async Task LoadForProfileAsync(string profileId)
    {
        ActiveProfileId = profileId;
        await LoadActiveDevicesAsync();
        await LoadTriggersAsync();
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
    private async Task LoadTriggersAsync()
    {
        if (string.IsNullOrEmpty(ActiveProfileId)) return;
        var list = await _api.ListTriggersAsync(ActiveProfileId);
        Triggers.Clear();
        foreach (var dto in list)
            Triggers.Add(TriggerEditModel.FromDto(dto));
        StatusMessage = $"{Triggers.Count} Trigger geladen.";
    }

    [RelayCommand]
    private void NewTrigger()
    {
        Editor = new TriggerEditModel { DeviceId = SelectedDevice?.DeviceId ?? string.Empty };
        IsEditorOpen = true;
    }

    [RelayCommand]
    private void EditTrigger(TriggerEditModel trigger)
    {
        Editor = TriggerEditModel.FromDto(trigger.ToDto());
        IsEditorOpen = true;
    }

    [RelayCommand]
    private void CancelEdit() { StopCapture(); IsEditorOpen = false; }

    [RelayCommand]
    private async Task SaveTriggerAsync()
    {
        if (string.IsNullOrEmpty(ActiveProfileId)) return;
        StopCapture();
        var dto      = Editor.ToDto();
        var existing = Triggers.FirstOrDefault(t => t.TriggerId == dto.TriggerId);
        bool ok;
        if (existing is null)
        {
            var created = await _api.CreateTriggerAsync(ActiveProfileId, dto);
            ok = created is not null;
            if (ok) Triggers.Add(TriggerEditModel.FromDto(created!));
        }
        else
        {
            ok = await _api.UpdateTriggerAsync(ActiveProfileId, dto);
            if (ok) Triggers[Triggers.IndexOf(existing)] = TriggerEditModel.FromDto(dto);
        }
        StatusMessage = ok ? "Trigger gespeichert." : "Fehler beim Speichern.";
        if (ok) IsEditorOpen = false;
    }

    [RelayCommand]
    private async Task DeleteTriggerAsync(TriggerEditModel trigger)
    {
        if (string.IsNullOrEmpty(ActiveProfileId)) return;
        var ok = await _api.DeleteTriggerAsync(ActiveProfileId, trigger.TriggerId);
        if (ok) Triggers.Remove(trigger);
        StatusMessage = ok ? "Trigger gelöscht." : "Fehler beim Löschen.";
    }

    [RelayCommand]
    private async Task StartCaptureAsync()
    {
        if (IsCapturing) return;
        IsCapturing   = true;
        CaptureStatus = "Warte auf MIDI-Event...";
        try
        {
            await _logHub.StartAsync();
            _midiSubscription = _logHub.On<MidiLogMessage>("MidiEventReceived", OnCapturedEvent);
        }
        catch (Exception ex)
        {
            CaptureStatus = $"Verbindung fehlgeschlagen: {ex.Message}";
            IsCapturing   = false;
        }
    }

    [RelayCommand]
    private void StopCapture()
    {
        _midiSubscription?.Dispose();
        _midiSubscription = null;
        IsCapturing       = false;
        CaptureStatus     = string.Empty;
    }

    private void OnCapturedEvent(MidiLogMessage msg)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            Editor.DeviceId    = msg.DeviceId;
            Editor.EventType   = msg.EventType;
            Editor.Channel     = msg.Channel;
            Editor.Data1Filter = msg.Data1;

            // MatchValue nur vorbelegen wenn ein konkreter Vergleichswert sinnvoll ist.
            // Bei DeltaData2/DD2* ist kein echter Delta aus einem Einzelevent berechenbar;
            // Data2 dient als Startwert.
            if (Editor.MatchMode != "Variable")
                Editor.MatchValue = msg.Data2;

            CaptureStatus = $"Erfasst: {msg.EventType} Ch{msg.Channel} #{msg.Data1}  Data2={msg.Data2}";
            StopCapture();
        });
    }
}
