namespace MidiController.Infrastructure.Input;

/// <summary>
/// Übersetzt Tasten-Namen (z. B. "ctrl", "F5", "a") in Windows Virtual-Key-Codes.
/// </summary>
public static class VirtualKeyMapper
{
    private static readonly Dictionary<string, ushort> _namedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        // Modifier
        ["ctrl"]        = 0x11,
        ["control"]     = 0x11,
        ["shift"]       = 0x10,
        ["alt"]         = 0x12,
        ["win"]         = 0x5B,
        ["lwin"]        = 0x5B,
        ["rwin"]        = 0x5C,

        // Navigation
        ["enter"]       = 0x0D,
        ["return"]      = 0x0D,
        ["escape"]      = 0x1B,
        ["esc"]         = 0x1B,
        ["space"]       = 0x20,
        ["backspace"]   = 0x08,
        ["tab"]         = 0x09,
        ["delete"]      = 0x2E,
        ["del"]         = 0x2E,
        ["insert"]      = 0x2D,
        ["home"]        = 0x24,
        ["end"]         = 0x23,
        ["pageup"]      = 0x21,
        ["pagedown"]    = 0x22,
        ["up"]          = 0x26,
        ["down"]        = 0x28,
        ["left"]        = 0x25,
        ["right"]       = 0x27,

        // Function keys
        ["f1"]  = 0x70, ["f2"]  = 0x71, ["f3"]  = 0x72, ["f4"]  = 0x73,
        ["f5"]  = 0x74, ["f6"]  = 0x75, ["f7"]  = 0x76, ["f8"]  = 0x77,
        ["f9"]  = 0x78, ["f10"] = 0x79, ["f11"] = 0x7A, ["f12"] = 0x7B,
        ["f13"] = 0x7C, ["f14"] = 0x7D, ["f15"] = 0x7E, ["f16"] = 0x7F,
        ["f17"] = 0x80, ["f18"] = 0x81, ["f19"] = 0x82, ["f20"] = 0x83,
        ["f21"] = 0x84, ["f22"] = 0x85, ["f23"] = 0x86, ["f24"] = 0x87,

        // Media
        ["mute"]        = 0xAD,
        ["volumedown"]  = 0xAE,
        ["volumeup"]    = 0xAF,
        ["medianext"]   = 0xB0,
        ["mediaprev"]   = 0xB1,
        ["mediastop"]   = 0xB2,
        ["mediaplay"]   = 0xB3,
        ["playpause"]   = 0xB3,

        // Numpad
        ["num0"] = 0x60, ["num1"] = 0x61, ["num2"] = 0x62, ["num3"] = 0x63,
        ["num4"] = 0x64, ["num5"] = 0x65, ["num6"] = 0x66, ["num7"] = 0x67,
        ["num8"] = 0x68, ["num9"] = 0x69,
        ["multiply"] = 0x6A, ["add"] = 0x6B, ["subtract"] = 0x6D,
        ["decimal"]  = 0x6E, ["divide"] = 0x6F,
        ["numlock"]  = 0x90,

        // Misc
        ["capslock"]    = 0x14,
        ["scrolllock"]  = 0x91,
        ["pause"]       = 0x13,
        ["printscreen"] = 0x2C,
        ["apps"]        = 0x5D,
    };

    /// <summary>
    /// Löst einen Tasten-Namen in einen Virtual-Key-Code auf.
    /// </summary>
    /// <param name="key">Tasten-Name, Einzel-Buchstabe oder Ziffer (Groß-/Kleinschreibung egal).</param>
    /// <returns>Windows VK-Code als ushort.</returns>
    /// <exception cref="ArgumentException">Wenn der Name nicht aufgelöst werden kann.</exception>
    public static ushort Resolve(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException($"Unbekannter Tasten-Name: '{key}'", nameof(key));

        if (_namedKeys.TryGetValue(key, out var vk))
            return vk;

        // Einzelner alphanumerischer Buchstabe oder Ziffer
        if (key.Length == 1)
        {
            char c = char.ToUpperInvariant(key[0]);
            if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                return (ushort)c;
        }

        throw new ArgumentException($"Unbekannter Tasten-Name: '{key}'", nameof(key));
    }
}
