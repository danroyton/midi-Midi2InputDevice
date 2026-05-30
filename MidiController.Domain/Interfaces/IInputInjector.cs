namespace MidiController.Domain.Interfaces;

/// <summary>
/// Injiziert Tastatur- und Mauseingaben in das Betriebssystem.
/// </summary>
public interface IInputInjector
{
    /// <summary>
    /// Sendet eine Tastenkombination mit optionaler Wiederholung, Hold-Dauer und Pause.
    /// </summary>
    /// <param name="keys">Tasten-Namen, z. B. ["Ctrl", "Alt", "F4"].</param>
    /// <param name="repeat">Anzahl Wiederholungen (X). 0 = kein Tastendruck.</param>
    /// <param name="holdDurationMs">Dauer des Tastendrucks in ms (Y). 0 = kurzer Tap.</param>
    /// <param name="pauseAfterMs">Pause nach dem Tastendruck in ms (Z).</param>
    void SendKeyCombination(string[] keys, int repeat, int holdDurationMs, int pauseAfterMs);
}
