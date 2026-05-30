using MidiControllerFrontend.ViewModels;
using MidiControllerFrontend.Views;
using System.Windows;
using System.Windows.Controls;

namespace MidiControllerFrontend;

public partial class MainWindow : Window
{
    private readonly StatusViewModel   _statusVm;
    private readonly MidiLogViewModel  _logVm;
    private readonly DevicesViewModel  _devicesVm;
    private readonly MappingsViewModel _mappingsVm;
    private readonly TemplatesViewModel _templatesVm;

    public MainWindow(
        MainViewModel      mainVm,
        StatusViewModel    statusVm,
        MidiLogViewModel   logVm,
        DevicesViewModel   devicesVm,
        MappingsViewModel  mappingsVm,
        TemplatesViewModel templatesVm)
    {
        _statusVm    = statusVm;
        _logVm       = logVm;
        _devicesVm   = devicesVm;
        _mappingsVm  = mappingsVm;
        _templatesVm = templatesVm;

        InitializeComponent();
        DataContext = mainVm;

        mainVm.NavigateCommand = new CommunityToolkit.Mvvm.Input.RelayCommand<string>(Navigate);

        // Beim Start zur Status-View navigieren
        Navigate("Status");
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        if (DataContext is MainViewModel vm)
            await vm.InitializeAsync();
    }

    private void Navigate(string? target)
    {
        ContentControl? area = ContentArea;
        if (area is null) return;

        area.Content = target switch
        {
            "Status"    => new StatusView    { DataContext = _statusVm    },
            "Log"       => new LogView       { DataContext = _logVm       },
            "Devices"   => new DevicesView   { DataContext = _devicesVm   },
            "Mappings"  => new MappingsView  { DataContext = _mappingsVm  },
            "Templates" => new TemplatesView { DataContext = _templatesVm },
            _           => new StatusView    { DataContext = _statusVm    }
        };
    }
}
