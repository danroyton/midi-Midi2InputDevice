namespace MidiControllerFrontend.Views;

/// <summary>
/// Statische Key-Listen für den KeyComboEditor.
/// Genau abgestimmt auf die in VirtualKeyMapper unterstützten Namen.
/// </summary>
public static class KeyDefinitions
{
    /// <summary>Leerer Eintrag für "kein Modifier gewählt".</summary>
    public const string None = "(keiner)";

    public static readonly IReadOnlyList<string> Modifiers =
    [
        None,
        "Ctrl",
        "Shift",
        "Alt",
        "Win",
    ];

    public static readonly IReadOnlyList<string> MainKeys =
    [
        // Alphabet
        "A","B","C","D","E","F","G","H","I","J","K","L","M",
        "N","O","P","Q","R","S","T","U","V","W","X","Y","Z",
        // Digits
        "0","1","2","3","4","5","6","7","8","9",
        // Function keys
        "F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12",
        "F13","F14","F15","F16","F17","F18","F19","F20","F21","F22","F23","F24",
        // Navigation
        "Enter","Escape","Space","Backspace","Tab","Delete","Insert",
        "Home","End","PageUp","PageDown",
        "Up","Down","Left","Right",
        // Numpad
        "Num0","Num1","Num2","Num3","Num4","Num5","Num6","Num7","Num8","Num9",
        "Multiply","Add","Subtract","Decimal","Divide","NumLock",
        // Media
        "Mute","VolumeDown","VolumeUp",
        "MediaNext","MediaPrev","MediaStop","MediaPlay",
        // Misc
        "CapsLock","ScrollLock","Pause","PrintScreen","Apps",
    ];

    /// <summary>
    /// Wandelt einen Modifier-Anzeigenamen (z.B. "Ctrl") in den VirtualKeyMapper-Namen ("ctrl").
    /// </summary>
    public static string ModifierToKeyName(string display) =>
        display.ToLowerInvariant();

    /// <summary>
    /// Wandelt einen Haupttasten-Anzeigenamen in den VirtualKeyMapper-Namen.
    /// (Einbuchstabige Tasten und Ziffern bleiben unverändert.)
    /// </summary>
    public static string MainKeyToKeyName(string display) =>
        display.ToLowerInvariant();

    /// <summary>
    /// Findet den Anzeigenamen eines Modifier aus einem VirtualKeyMapper-Namen.
    /// Gibt <see cref="None"/> zurück wenn nicht gefunden.
    /// </summary>
    public static string KeyNameToModifier(string keyName)
    {
        var lower = keyName.ToLowerInvariant();
        return lower switch
        {
            "ctrl" or "control" => "Ctrl",
            "shift"             => "Shift",
            "alt"               => "Alt",
            "win" or "lwin" or "rwin" => "Win",
            _                   => None
        };
    }

    /// <summary>
    /// Findet den Anzeigenamen einer Haupttaste aus einem VirtualKeyMapper-Namen.
    /// Gibt null zurück wenn nicht gefunden.
    /// </summary>
    public static string? KeyNameToMainKey(string keyName)
    {
        var lower = keyName.ToLowerInvariant();
        // Einbuchstabige Buchstaben/Ziffern → Großbuchstabe
        if (keyName.Length == 1 && (char.IsLetterOrDigit(keyName[0])))
            return keyName.ToUpperInvariant();

        return MainKeys.FirstOrDefault(k =>
            k.Equals(keyName, StringComparison.OrdinalIgnoreCase));
    }
}
