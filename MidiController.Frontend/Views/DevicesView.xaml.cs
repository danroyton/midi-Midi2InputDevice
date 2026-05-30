using MidiControllerFrontend.ViewModels;
using System.Windows.Controls;

namespace MidiControllerFrontend.Views;

public partial class DevicesView : UserControl
{
    public DevicesView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is DevicesViewModel vm)
                await vm.RefreshAsync();
        };
    }
}
