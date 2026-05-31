using System.Windows;
using System.Windows.Controls;

namespace MidiControllerFrontend.Views;

/// <summary>
/// Strukturierter Eingabe-Editor für Tastenkombinationen.
/// Ersetzt das freie Text-Feld durch zwei Modifier-ComboBoxen und eine Haupttasten-ComboBox.
/// Bindet bidirektional an einen komma-getrennten Keys-String (Format des VirtualKeyMapper).
/// </summary>
public partial class KeyComboEditor : UserControl
{
    private bool _isSyncing; // verhindert Rückkopplungsschleifen

    public KeyComboEditor()
    {
        InitializeComponent();
    }

    // ── Öffentliche Item-Quellen ──────────────────────────────────────────────

    public IReadOnlyList<string> Modifiers => KeyDefinitions.Modifiers;
    public IReadOnlyList<string> MainKeys  => KeyDefinitions.MainKeys;

    // ── KeysValue – TwoWay-bindbar an ActionEditModel.Keys ──────────────────

    public static readonly DependencyProperty KeysValueProperty =
        DependencyProperty.Register(
            nameof(KeysValue),
            typeof(string),
            typeof(KeyComboEditor),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnKeysValueChanged));

    public string KeysValue
    {
        get => (string)GetValue(KeysValueProperty);
        set => SetValue(KeysValueProperty, value);
    }

    private static void OnKeysValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (KeyComboEditor)d;
        if (!ctrl._isSyncing)
            ctrl.ParseKeysValue((string)e.NewValue);
    }

    // ── Interne Selections ───────────────────────────────────────────────────

    public static readonly DependencyProperty SelectedModifier1Property =
        DependencyProperty.Register(nameof(SelectedModifier1), typeof(string), typeof(KeyComboEditor),
            new PropertyMetadata(KeyDefinitions.None, OnSelectionChanged));

    public static readonly DependencyProperty SelectedModifier2Property =
        DependencyProperty.Register(nameof(SelectedModifier2), typeof(string), typeof(KeyComboEditor),
            new PropertyMetadata(KeyDefinitions.None, OnSelectionChanged));

    public static readonly DependencyProperty SelectedMainKeyProperty =
        DependencyProperty.Register(nameof(SelectedMainKey), typeof(string), typeof(KeyComboEditor),
            new PropertyMetadata(null, OnSelectionChanged));

    public string  SelectedModifier1 { get => (string) GetValue(SelectedModifier1Property); set => SetValue(SelectedModifier1Property, value); }
    public string  SelectedModifier2 { get => (string) GetValue(SelectedModifier2Property); set => SetValue(SelectedModifier2Property, value); }
    public string? SelectedMainKey   { get => (string?)GetValue(SelectedMainKeyProperty);   set => SetValue(SelectedMainKeyProperty,   value); }

    // ── Sichtbarkeits-Hilfsproperties für +Trennzeichen ─────────────────────

    public static readonly DependencyProperty Modifier1ActiveProperty =
        DependencyProperty.Register(nameof(Modifier1Active), typeof(bool), typeof(KeyComboEditor),
            new PropertyMetadata(false));

    public static readonly DependencyProperty AnyModifierActiveProperty =
        DependencyProperty.Register(nameof(AnyModifierActive), typeof(bool), typeof(KeyComboEditor),
            new PropertyMetadata(false));

    public bool Modifier1Active   { get => (bool)GetValue(Modifier1ActiveProperty);   set => SetValue(Modifier1ActiveProperty,   value); }
    public bool AnyModifierActive { get => (bool)GetValue(AnyModifierActiveProperty); set => SetValue(AnyModifierActiveProperty, value); }

    // ── Vorschau ─────────────────────────────────────────────────────────────

    public static readonly DependencyProperty PreviewProperty =
        DependencyProperty.Register(nameof(Preview), typeof(string), typeof(KeyComboEditor),
            new PropertyMetadata(string.Empty));

    public string Preview { get => (string)GetValue(PreviewProperty); set => SetValue(PreviewProperty, value); }

    // ── Logik ────────────────────────────────────────────────────────────────

    private static void OnSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (KeyComboEditor)d;
        ctrl.RebuildKeysValue();
    }

    /// <summary>Zerlegt einen komma-getrennten Keys-String in Modifier1/Modifier2/MainKey.</summary>
    private void ParseKeysValue(string raw)
    {
        _isSyncing = true;
        try
        {
            var parts = raw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            var modifiers = parts
                .Select(p => KeyDefinitions.KeyNameToModifier(p))
                .Where(m => m != KeyDefinitions.None)
                .ToList();

            var mainKeyRaw = parts
                .FirstOrDefault(p => KeyDefinitions.KeyNameToModifier(p) == KeyDefinitions.None);

            SelectedModifier1 = modifiers.Count > 0 ? modifiers[0] : KeyDefinitions.None;
            SelectedModifier2 = modifiers.Count > 1 ? modifiers[1] : KeyDefinitions.None;
            SelectedMainKey   = mainKeyRaw is not null
                ? KeyDefinitions.KeyNameToMainKey(mainKeyRaw)
                : null;

            UpdateHelperProps();
        }
        finally
        {
            _isSyncing = false;
        }
    }

    /// <summary>Baut aus den drei Selections den komma-getrennten Keys-String und schreibt ihn zurück.</summary>
    private void RebuildKeysValue()
    {
        if (_isSyncing) return;

        var parts = new List<string>(3);

        if (SelectedModifier1 != KeyDefinitions.None)
            parts.Add(KeyDefinitions.ModifierToKeyName(SelectedModifier1));

        if (SelectedModifier2 != KeyDefinitions.None
            && SelectedModifier2 != SelectedModifier1)
            parts.Add(KeyDefinitions.ModifierToKeyName(SelectedModifier2));

        if (!string.IsNullOrEmpty(SelectedMainKey))
            parts.Add(KeyDefinitions.MainKeyToKeyName(SelectedMainKey));

        _isSyncing = true;
        try
        {
            KeysValue = string.Join(", ", parts);
        }
        finally
        {
            _isSyncing = false;
        }

        UpdateHelperProps();
    }

    private void UpdateHelperProps()
    {
        Modifier1Active   = SelectedModifier1 != KeyDefinitions.None;
        AnyModifierActive = SelectedModifier1 != KeyDefinitions.None
                         || SelectedModifier2 != KeyDefinitions.None;

        // Vorschau
        Preview = string.IsNullOrWhiteSpace(KeysValue)
            ? "Keine Taste gewählt"
            : $"→  {KeysValue}";
    }
}
