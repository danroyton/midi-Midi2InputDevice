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

    partial void OnKeysChanged(string value)        => OnPropertyChanged(nameof(Summary));
    partial void OnRepeatCountChanged(int value)    => OnPropertyChanged(nameof(Summary));
    partial void OnDurationMsChanged(int value)     => OnPropertyChanged(nameof(Summary));
    partial void OnPauseMsChanged(int value)        => OnPropertyChanged(nameof(Summary));

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
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set { if (SetProperty(ref _name, value)) OnPropertyChanged(nameof(Summary)); }
    }
    [ObservableProperty] private string _deviceId    = string.Empty;
    [ObservableProperty] private string _eventType   = "ControlChange";
    [ObservableProperty] private int    _channel     = 1;
    [ObservableProperty] private int?   _data1Filter;
    [ObservableProperty] private string _matchMode   = "Variable";
    [ObservableProperty] private int    _matchValue;
    [ObservableProperty] private bool   _matchValueVisible;

    // ── Aktionen ──────────────────────────────────────────────────────────────
    [ObservableProperty] private ActionEditModel? _selectedAction;
    public ObservableCollection<ActionEditModel> Actions { get; } = new();

    // ── Pre-Phase ─────────────────────────────────────────────────────────────
    [ObservableProperty] private StateAssignmentEditModel? _selectedPreAssignment;
    [ObservableProperty] private MidiSendEditModel?        _selectedPreMidiSend;
    public ObservableCollection<StateAssignmentEditModel> PreAssignments { get; } = new();
    public ObservableCollection<MidiSendEditModel>        PreMidiSend    { get; } = new();

    // ── Bedingungen ───────────────────────────────────────────────────────────
    [ObservableProperty] private ConditionBlockEditModel? _selectedConditionBlock;
    public ObservableCollection<ConditionBlockEditModel> ConditionBlocks { get; } = new();

    // ── Post-Phase ────────────────────────────────────────────────────────────
    [ObservableProperty] private StateAssignmentEditModel? _selectedPostAssignment;
    [ObservableProperty] private MidiSendEditModel?        _selectedPostMidiSend;
    public ObservableCollection<StateAssignmentEditModel> PostAssignments { get; } = new();
    public ObservableCollection<MidiSendEditModel>        PostMidiSend    { get; } = new();

    // ── Else ──────────────────────────────────────────────────────────────────
    [ObservableProperty] private bool              _hasElse;
    [ObservableProperty] private ElseConfigEditModel _else = new();

    // ── Computed ──────────────────────────────────────────────────────────────
    public string Summary =>
        $"{DeviceId}  Ch{Channel}  {EventType}" +
        (Data1Filter.HasValue ? $"  #{Data1Filter}" : "") +
        $"  [{MatchMode}{(MatchMode != "Variable" ? $"={MatchValue}" : "")}]";

    public string KeysSummary =>
        Actions.Count == 0
            ? string.Empty
            : string.Join(" | ", Actions.Select(a => string.IsNullOrWhiteSpace(a.Keys) ? "?" : a.Keys));

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

    // ── Aktions-Commands ──────────────────────────────────────────────────────
    [RelayCommand]
    private void AddAction()
    { var a = new ActionEditModel(); Actions.Add(a); SelectedAction = a; }

    [RelayCommand]
    private void RemoveAction(ActionEditModel action) => Actions.Remove(action);

    // ── Pre-Commands ──────────────────────────────────────────────────────────
    [RelayCommand]
    private void AddPreAssignment()
    { var s = new StateAssignmentEditModel(); PreAssignments.Add(s); SelectedPreAssignment = s; }

    [RelayCommand]
    private void RemovePreAssignment(StateAssignmentEditModel s) => PreAssignments.Remove(s);

    [RelayCommand]
    private void AddPreMidiSend()
    { var m = new MidiSendEditModel(); PreMidiSend.Add(m); SelectedPreMidiSend = m; }

    [RelayCommand]
    private void RemovePreMidiSend(MidiSendEditModel m) => PreMidiSend.Remove(m);

    // ── Condition-Commands ────────────────────────────────────────────────────
    [RelayCommand]
    private void AddConditionBlock()
    { var b = new ConditionBlockEditModel(); ConditionBlocks.Add(b); SelectedConditionBlock = b; }

    [RelayCommand]
    private void RemoveConditionBlock(ConditionBlockEditModel b) => ConditionBlocks.Remove(b);

    // ── Post-Commands ─────────────────────────────────────────────────────────
    [RelayCommand]
    private void AddPostAssignment()
    { var s = new StateAssignmentEditModel(); PostAssignments.Add(s); SelectedPostAssignment = s; }

    [RelayCommand]
    private void RemovePostAssignment(StateAssignmentEditModel s) => PostAssignments.Remove(s);

    [RelayCommand]
    private void AddPostMidiSend()
    { var m = new MidiSendEditModel(); PostMidiSend.Add(m); SelectedPostMidiSend = m; }

    [RelayCommand]
    private void RemovePostMidiSend(MidiSendEditModel m) => PostMidiSend.Remove(m);

    // ── ToDto / FromDto ───────────────────────────────────────────────────────
    public TriggerDto ToDto()
    {
        var n = Name;
        return new(
            TriggerId:             TriggerId,
            DeviceId:              DeviceId,
            EventType:             EventType,
            Channel:               Channel,
            Data1Filter:           Data1Filter,
            MatchMode:             MatchMode,
            MatchValue:            MatchValue,
            GlobalPreAssignments:  [.. PreAssignments.Select(s => s.ToDto())],
            ConditionBlocks:       [.. ConditionBlocks.Select(b => b.ToDto())],
            Actions:               [.. Actions.Select(a => a.ToDto())],
            GlobalPostAssignments: [.. PostAssignments.Select(s => s.ToDto())],
            ElseConfig:            HasElse ? Else.ToDto() : null
        )
        {
            Name               = n,
            GlobalPreMidiSend  = [.. PreMidiSend.Select(m => m.ToDto())],
            GlobalPostMidiSend = [.. PostMidiSend.Select(m => m.ToDto())],
        };
    }

    public static TriggerEditModel FromDto(TriggerDto dto)
    {
        var m = new TriggerEditModel
        {
            TriggerId         = dto.TriggerId,
            Name              = dto.Name,
            DeviceId          = dto.DeviceId,
            EventType         = dto.EventType,
            Channel           = dto.Channel,
            Data1Filter       = dto.Data1Filter,
            MatchMode         = dto.MatchMode,
            MatchValue        = dto.MatchValue,
            MatchValueVisible = dto.MatchMode != "Variable",
            HasElse           = dto.ElseConfig is not null,
        };
        foreach (var s in dto.GlobalPreAssignments)  m.PreAssignments.Add(StateAssignmentEditModel.FromDto(s));
        foreach (var s in dto.GlobalPreMidiSend)      m.PreMidiSend.Add(MidiSendEditModel.FromDto(s));
        foreach (var b in dto.ConditionBlocks)        m.ConditionBlocks.Add(ConditionBlockEditModel.FromDto(b));
        foreach (var a in dto.Actions)                m.Actions.Add(ActionEditModel.FromDto(a));
        foreach (var s in dto.GlobalPostAssignments)  m.PostAssignments.Add(StateAssignmentEditModel.FromDto(s));
        foreach (var s in dto.GlobalPostMidiSend)     m.PostMidiSend.Add(MidiSendEditModel.FromDto(s));
        if (dto.ElseConfig is not null) m.Else = ElseConfigEditModel.FromDto(dto.ElseConfig);
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
