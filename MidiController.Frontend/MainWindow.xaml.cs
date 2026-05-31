using MidiControllerFrontend.ViewModels;
using MidiControllerFrontend.Views;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace MidiControllerFrontend;

public partial class MainWindow : Window
{
    private readonly MainViewModel     _mainVm;
    private readonly StatusViewModel   _statusVm;
    private readonly MidiLogViewModel  _logVm;
    private readonly DevicesViewModel  _devicesVm;
    private readonly MappingsViewModel _mappingsVm;
    private readonly TemplatesViewModel _templatesVm;
    private readonly KeyTestViewModel  _keyTestVm;
    private string? _currentNav;

    public MainWindow(
        MainViewModel      mainVm,
        StatusViewModel    statusVm,
        MidiLogViewModel   logVm,
        DevicesViewModel   devicesVm,
        MappingsViewModel  mappingsVm,
        TemplatesViewModel templatesVm,
        KeyTestViewModel   keyTestVm)
    {
        _mainVm      = mainVm;
        _statusVm    = statusVm;
        _logVm       = logVm;
        _devicesVm   = devicesVm;
        _mappingsVm  = mappingsVm;
        _templatesVm = templatesVm;
        _keyTestVm   = keyTestVm;

        InitializeComponent();
        DataContext = mainVm;

        mainVm.NavigateCommand = new CommunityToolkit.Mvvm.Input.RelayCommand<string>(Navigate);

        // Wenn das Profil wechselt und Mappings gerade aktiv ist → neu laden
        mainVm.PropertyChanged += OnMainVmPropertyChanged;

        Navigate("Status");
    }

    // ── Tray-Verhalten ───────────────────────────────────────────────────────

    protected override void OnClosing(CancelEventArgs e)
    {
        // Fenster nicht wirklich schließen – in Tray verstecken
        e.Cancel = true;
        Hide();
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (WindowState == WindowState.Minimized)
            Hide();
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        await _mainVm.InitializeAsync();
    }

    private async void OnMainVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedProfile)
            && _currentNav == "Mappings"
            && _mainVm.SelectedProfile is { } profileId)
        {
            await _mappingsVm.LoadForProfileAsync(profileId);
        }
    }

    private async void Navigate(string? target)
    {
        ContentControl? area = ContentArea;
        if (area is null) return;

        _currentNav = target;

        area.Content = target switch
        {
            "Status"    => new StatusView    { DataContext = _statusVm    },
            "Log"       => new LogView       { DataContext = _logVm       },
            "Devices"   => new DevicesView   { DataContext = _devicesVm   },
            "Mappings"  => new MappingsView  { DataContext = _mappingsVm  },
            "Templates" => new TemplatesView { DataContext = _templatesVm },
            "KeyTest"   => new KeyTestView   { DataContext = _keyTestVm   },
            _           => new StatusView    { DataContext = _statusVm    }
        };

        // Beim Wechsel zu Mappings sofort Daten des aktuellen Profils laden
        if (target == "Mappings" && _mainVm.SelectedProfile is { } profileId)
            await _mappingsVm.LoadForProfileAsync(profileId);
    }
}
