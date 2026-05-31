using System.Collections;
using System.Windows;
using System.Windows.Controls;
using MidiControllerFrontend.Services;

namespace MidiControllerFrontend.Views;

public partial class MidiSendEditor : UserControl
{
    public static readonly DependencyProperty ActiveDevicesProperty =
        DependencyProperty.Register(nameof(ActiveDevices), typeof(IEnumerable),
            typeof(MidiSendEditor), new PropertyMetadata(null));

    public IEnumerable? ActiveDevices
    {
        get => (IEnumerable?)GetValue(ActiveDevicesProperty);
        set => SetValue(ActiveDevicesProperty, value);
    }

    public MidiSendEditor() => InitializeComponent();
}
