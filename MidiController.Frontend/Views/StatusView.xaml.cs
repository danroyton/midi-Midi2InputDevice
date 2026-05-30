using MidiControllerFrontend.ViewModels;
using System.Windows.Controls;

namespace MidiControllerFrontend.Views;

public partial class StatusView : UserControl
{
    public StatusView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is StatusViewModel vm)
                await vm.AutoConnectAsync();
        };
    }
}
