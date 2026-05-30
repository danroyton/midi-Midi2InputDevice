using MidiControllerFrontend.ViewModels;
using System.Collections.Specialized;
using System.Windows.Controls;

namespace MidiControllerFrontend.Views;

public partial class LogView : UserControl
{
    private DataGrid? _grid;

    public LogView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            _grid = FindName("LogGrid") as DataGrid;
            if (DataContext is MidiLogViewModel vm)
            {
                vm.Entries.CollectionChanged += OnEntriesChanged;
                await vm.AutoConnectAsync();
            }
        };
        Unloaded += (_, _) =>
        {
            if (DataContext is MidiLogViewModel vm)
                vm.Entries.CollectionChanged -= OnEntriesChanged;
        };
    }

    private void OnEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && _grid?.Items.Count > 0)
            _grid.ScrollIntoView(_grid.Items[^1]);
    }
}
