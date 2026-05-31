using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MidiControllerFrontend.ViewModels;

public sealed record KeyEntry(string Time, string Action, string Key, string Modifiers);

public sealed partial class KeyTestViewModel : ObservableObject
{
    private const int MaxEntries = 200;

    public ObservableCollection<KeyEntry> Entries { get; } = new();

    public bool IsListening
    {
        get => _isListening;
        private set => SetProperty(ref _isListening, value);
    }
    private bool _isListening;

    [RelayCommand]
    private void ToggleListen() => IsListening = !IsListening;

    [RelayCommand]
    private void Clear() => Entries.Clear();

    public void RegisterKey(KeyEventArgs e, string action)
    {
        if (!IsListening) return;

        var mods = new System.Text.StringBuilder();
        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) mods.Append("Ctrl+");
        if ((Keyboard.Modifiers & ModifierKeys.Alt)     != 0) mods.Append("Alt+");
        if ((Keyboard.Modifiers & ModifierKeys.Shift)   != 0) mods.Append("Shift+");
        if ((Keyboard.Modifiers & ModifierKeys.Windows) != 0) mods.Append("Win+");

        var entry = new KeyEntry(
            Time:      DateTime.Now.ToString("HH:mm:ss.fff"),
            Action:    action,
            Key:       e.Key.ToString(),
            Modifiers: mods.Length > 0 ? mods.ToString().TrimEnd('+') : "–");

        Entries.Insert(0, entry);

        while (Entries.Count > MaxEntries)
            Entries.RemoveAt(Entries.Count - 1);
    }
}
