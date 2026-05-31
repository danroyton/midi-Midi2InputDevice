using MidiControllerFrontend.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace MidiControllerFrontend.Views;

public partial class KeyTestView : UserControl
{
    public KeyTestView()
    {
        InitializeComponent();

        // Tastaturevents am InputField abfangen und ans ViewModel weiterleiten
        InputField.PreviewKeyDown += (_, e) =>
        {
            if (DataContext is KeyTestViewModel vm)
                vm.RegisterKey(e, "KeyDown");
            e.Handled = true;
        };

        InputField.PreviewKeyUp += (_, e) =>
        {
            if (DataContext is KeyTestViewModel vm)
                vm.RegisterKey(e, "KeyUp");
            e.Handled = true;
        };
    }
}
