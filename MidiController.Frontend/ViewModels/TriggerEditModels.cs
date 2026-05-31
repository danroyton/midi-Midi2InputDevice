using CommunityToolkit.Mvvm.Input;
using MidiControllerFrontend.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MidiControllerFrontend.ViewModels;

// ── Hilfslisten ────────────────────────────────────────────────────────────────

/// <summary>Alle ValueSource-Namen die als Ziel einer StateAssignment erlaubt sind.</summary>
public static class ValueSourceNames
{
    public static readonly IReadOnlyList<string> WritableSources =
    [
        "Fixed",
        "MidiData1", "MidiData2", "DeltaData2",
        "DD2Positive", "DD2Negative",
        "VariableA","VariableB","VariableC","VariableD","VariableE",
        "VariableF","VariableG","VariableH","VariableI","VariableJ",
        "VariableK","VariableL","VariableM","VariableN","VariableO",
        "VariableP","VariableQ","VariableR","VariableS","VariableT",
        "VariableU","VariableV","VariableW","VariableX","VariableY","VariableZ",
    ];

    public static readonly IReadOnlyList<string> ConditionSources =
    [
        "Fixed",
        "MidiData1", "MidiData2", "DeltaData2",
        "DD2Positive", "DD2Negative",
        "VariableA","VariableB","VariableC","VariableD","VariableE",
        "VariableF","VariableG","VariableH","VariableI","VariableJ",
        "VariableK","VariableL","VariableM","VariableN","VariableO",
        "VariableP","VariableQ","VariableR","VariableS","VariableT",
        "VariableU","VariableV","VariableW","VariableX","VariableY","VariableZ",
    ];

    public static readonly IReadOnlyList<string> Variables =
        Enumerable.Range('A', 26).Select(c => ((char)c).ToString()).ToList();

    public static readonly IReadOnlyList<string> CompareOps =
        ["==", "!=", "<", ">", "<=", ">="];

    public static readonly IReadOnlyList<string> MidiEventTypes =
        ["NoteOn", "NoteOff", "ControlChange", "ProgramChange", "PitchBend", "ChannelAftertouch"];
}

// ── Basisklasse ───────────────────────────────────────────────────────────────

public abstract class NotifyBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    protected void Notify([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

// ── StateAssignmentEditModel ──────────────────────────────────────────────────

public sealed class StateAssignmentEditModel : NotifyBase
{
    private string _variable   = "A";
    private string _source     = "Fixed";
    private int    _fixedValue;
    private bool   _fixedVisible = true;

    public string Variable
    {
        get => _variable;
        set { Set(ref _variable, value); Notify(nameof(Summary)); }
    }
    public string Source
    {
        get => _source;
        set
        {
            Set(ref _source, value);
            FixedVisible = value == "Fixed";
            Notify(nameof(Summary));
        }
    }
    public int FixedValue
    {
        get => _fixedValue;
        set { Set(ref _fixedValue, value); Notify(nameof(Summary)); }
    }
    public bool FixedVisible
    {
        get => _fixedVisible;
        set => Set(ref _fixedVisible, value);
    }

    public string Summary => $"{Variable} ← {(Source == "Fixed" ? FixedValue.ToString() : Source)}";

    public StateAssignmentDto ToDto() => new(Variable[0], Source, FixedValue);

    public static StateAssignmentEditModel FromDto(StateAssignmentDto dto) => new()
    {
        Variable     = dto.Variable.ToString(),
        Source       = dto.Source,
        FixedValue   = dto.FixedValue,
    };
}

// ── MidiSendEditModel ─────────────────────────────────────────────────────────

public sealed class MidiSendEditModel : NotifyBase
{
    private string _deviceId  = string.Empty;
    private string _eventType = "ControlChange";
    private int    _channel   = 1;
    private int    _data1;
    private int    _data2;

    public string DeviceId
    {
        get => _deviceId;
        set { Set(ref _deviceId, value); Notify(nameof(Summary)); }
    }
    public string EventType
    {
        get => _eventType;
        set { Set(ref _eventType, value); Notify(nameof(Summary)); }
    }
    public int Channel
    {
        get => _channel;
        set { Set(ref _channel, value); Notify(nameof(Summary)); }
    }
    public int Data1
    {
        get => _data1;
        set { Set(ref _data1, value); Notify(nameof(Summary)); }
    }
    public int Data2
    {
        get => _data2;
        set { Set(ref _data2, value); Notify(nameof(Summary)); }
    }

    public string Summary => $"{DeviceId}  {EventType}  Ch{Channel}  #{Data1}={Data2}";

    public MidiSendCommandDto ToDto() => new(DeviceId, EventType, Channel, Data1, Data2);

    public static MidiSendEditModel FromDto(MidiSendCommandDto dto) => new()
    {
        DeviceId  = dto.DeviceId,
        EventType = dto.EventType,
        Channel   = dto.Channel,
        Data1     = dto.Data1,
        Data2     = dto.Data2,
    };
}

// ── ConditionEditModel ────────────────────────────────────────────────────────

public sealed class ConditionEditModel : NotifyBase
{
    private string _left             = "MidiData2";
    private string _op               = "==";
    private string _rightSource      = "Fixed";
    private int    _rightFixed;
    private bool   _rightFixedVisible = true;

    public string Left
    {
        get => _left;
        set { Set(ref _left, value); Notify(nameof(Summary)); }
    }
    public string Op
    {
        get => _op;
        set { Set(ref _op, value); Notify(nameof(Summary)); }
    }
    public string RightSource
    {
        get => _rightSource;
        set
        {
            Set(ref _rightSource, value);
            RightFixedVisible = value == "Fixed";
            Notify(nameof(Summary));
        }
    }
    public int RightFixed
    {
        get => _rightFixed;
        set { Set(ref _rightFixed, value); Notify(nameof(Summary)); }
    }
    public bool RightFixedVisible
    {
        get => _rightFixedVisible;
        set => Set(ref _rightFixedVisible, value);
    }

    public string Summary =>
        $"{Left} {Op} {(RightSource == "Fixed" ? RightFixed.ToString() : RightSource)}";

    public ConditionDto ToDto() => new(Left, Op, RightSource, RightFixed);

    public static ConditionEditModel FromDto(ConditionDto dto) => new()
    {
        Left        = dto.Left,
        Op          = dto.Op,
        RightSource = dto.RightSource,
        RightFixed  = dto.RightFixed,
    };
}

// ── ConditionBlockEditModel ───────────────────────────────────────────────────

public sealed class ConditionBlockEditModel : NotifyBase
{
    private string? _templateName;
    private bool    _useTemplate;
    private ConditionEditModel? _selectedCondition;

    public string? TemplateName
    {
        get => _templateName;
        set { Set(ref _templateName, value); Notify(nameof(Summary)); }
    }
    public bool UseTemplate
    {
        get => _useTemplate;
        set { Set(ref _useTemplate, value); Notify(nameof(Summary)); }
    }
    public ConditionEditModel? SelectedCondition
    {
        get => _selectedCondition;
        set => Set(ref _selectedCondition, value);
    }

    public ObservableCollection<ConditionEditModel> Conditions { get; } = new();

    public string Summary =>
        UseTemplate && !string.IsNullOrWhiteSpace(TemplateName)
            ? $"Template: {TemplateName}"
            : $"Block ({Conditions.Count} Bed.)";

    private RelayCommand? _addConditionCommand;
    public RelayCommand AddConditionCommand => _addConditionCommand ??= new RelayCommand(() =>
    {
        var c = new ConditionEditModel();
        Conditions.Add(c);
        SelectedCondition = c;
    });

    private RelayCommand<ConditionEditModel>? _removeConditionCommand;
    public RelayCommand<ConditionEditModel> RemoveConditionCommand =>
        _removeConditionCommand ??= new RelayCommand<ConditionEditModel>(c => Conditions.Remove(c!));

    public ConditionBlockDto ToDto()
    {
        if (UseTemplate && !string.IsNullOrWhiteSpace(TemplateName))
            return new(TemplateName, []);
        return new(null, [.. Conditions.Select(c => c.ToDto())]);
    }

    public static ConditionBlockEditModel FromDto(ConditionBlockDto dto)
    {
        var m = new ConditionBlockEditModel
        {
            UseTemplate  = dto.TemplateName is not null,
            TemplateName = dto.TemplateName,
        };
        foreach (var c in dto.Conditions)
            m.Conditions.Add(ConditionEditModel.FromDto(c));
        return m;
    }
}

// ── ElseConfigEditModel ───────────────────────────────────────────────────────

public sealed class ElseConfigEditModel : NotifyBase
{
    private ConditionBlockEditModel? _selectedConditionBlock;
    private ActionEditModel?         _selectedAction;
    private StateAssignmentEditModel? _selectedPostAssignment;
    private MidiSendEditModel?        _selectedPostMidiSend;

    public ObservableCollection<ConditionBlockEditModel>  ConditionBlocks  { get; } = new();
    public ObservableCollection<ActionEditModel>          Actions          { get; } = new();
    public ObservableCollection<StateAssignmentEditModel> PostAssignments  { get; } = new();
    public ObservableCollection<MidiSendEditModel>        PostMidiSend     { get; } = new();

    public ConditionBlockEditModel? SelectedConditionBlock
    {
        get => _selectedConditionBlock;
        set => Set(ref _selectedConditionBlock, value);
    }
    public ActionEditModel? SelectedAction
    {
        get => _selectedAction;
        set => Set(ref _selectedAction, value);
    }
    public StateAssignmentEditModel? SelectedPostAssignment
    {
        get => _selectedPostAssignment;
        set => Set(ref _selectedPostAssignment, value);
    }
    public MidiSendEditModel? SelectedPostMidiSend
    {
        get => _selectedPostMidiSend;
        set => Set(ref _selectedPostMidiSend, value);
    }

    private RelayCommand? _addConditionBlockCommand;
    public RelayCommand AddConditionBlockCommand => _addConditionBlockCommand ??= new RelayCommand(() =>
    { var b = new ConditionBlockEditModel(); ConditionBlocks.Add(b); SelectedConditionBlock = b; });

    private RelayCommand<ConditionBlockEditModel>? _removeConditionBlockCommand;
    public RelayCommand<ConditionBlockEditModel> RemoveConditionBlockCommand =>
        _removeConditionBlockCommand ??= new RelayCommand<ConditionBlockEditModel>(b => ConditionBlocks.Remove(b!));

    private RelayCommand? _addActionCommand;
    public RelayCommand AddActionCommand => _addActionCommand ??= new RelayCommand(() =>
    { var a = new ActionEditModel(); Actions.Add(a); SelectedAction = a; });

    private RelayCommand<ActionEditModel>? _removeActionCommand;
    public RelayCommand<ActionEditModel> RemoveActionCommand =>
        _removeActionCommand ??= new RelayCommand<ActionEditModel>(a => Actions.Remove(a!));

    private RelayCommand? _addPostAssignmentCommand;
    public RelayCommand AddPostAssignmentCommand => _addPostAssignmentCommand ??= new RelayCommand(() =>
    { var s = new StateAssignmentEditModel(); PostAssignments.Add(s); SelectedPostAssignment = s; });

    private RelayCommand<StateAssignmentEditModel>? _removePostAssignmentCommand;
    public RelayCommand<StateAssignmentEditModel> RemovePostAssignmentCommand =>
        _removePostAssignmentCommand ??= new RelayCommand<StateAssignmentEditModel>(s => PostAssignments.Remove(s!));

    private RelayCommand? _addPostMidiSendCommand;
    public RelayCommand AddPostMidiSendCommand => _addPostMidiSendCommand ??= new RelayCommand(() =>
    { var m = new MidiSendEditModel(); PostMidiSend.Add(m); SelectedPostMidiSend = m; });

    private RelayCommand<MidiSendEditModel>? _removePostMidiSendCommand;
    public RelayCommand<MidiSendEditModel> RemovePostMidiSendCommand =>
        _removePostMidiSendCommand ??= new RelayCommand<MidiSendEditModel>(m => PostMidiSend.Remove(m!));

    public TriggerConfigDto ToDto() => new(
        ConditionBlocks:       [.. ConditionBlocks.Select(b => b.ToDto())],
        Actions:               [.. Actions.Select(a => a.ToDto())],
        GlobalPostAssignments: [.. PostAssignments.Select(s => s.ToDto())]
    );

    public static ElseConfigEditModel FromDto(TriggerConfigDto dto)
    {
        var m = new ElseConfigEditModel();
        foreach (var b in dto.ConditionBlocks)       m.ConditionBlocks.Add(ConditionBlockEditModel.FromDto(b));
        foreach (var a in dto.Actions)               m.Actions.Add(ActionEditModel.FromDto(a));
        foreach (var s in dto.GlobalPostAssignments) m.PostAssignments.Add(StateAssignmentEditModel.FromDto(s));
        return m;
    }
}
