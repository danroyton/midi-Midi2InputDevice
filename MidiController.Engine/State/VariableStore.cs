using MidiController.Domain.State;

namespace MidiController.Engine.State;

/// <summary>
/// Thread-sicherer Speicher für die persistenten Zustandsvariablen A–Z.
/// Lese- und Schreibzugriff erfolgt über Buchstaben ('A'–'Z').
/// Feuert <see cref="VariableChanged"/> wenn ein Wert sich tatsächlich ändert.
/// </summary>
public sealed class VariableStore
{
    private readonly int[] _values = new int[26];
    private readonly Lock _lock = new();

    /// <summary>
    /// Wird gefeuert wenn eine Variable ihren Wert ändert.
    /// Parameter: (variable, neuerWert).
    /// Das Event wird außerhalb des Locks ausgelöst.
    /// </summary>
    public event Action<char, int>? VariableChanged;

    public VariableStore()
    {
        Reset();
    }

    /// <summary>Setzt alle Variablen auf ihre Initialwerte zurück.</summary>
    public void Reset()
    {
        lock (_lock)
        {
            Array.Fill(_values, 0);
            foreach (var (variable, initialValue) in EngineState.InitialValues)
                _values[IndexOf(variable)] = initialValue;
        }
        // Snapshot nach Reset broadcasten – alle Variablen als geändert melden
        for (int i = 0; i < 26; i++)
        {
            char v = (char)('A' + i);
            VariableChanged?.Invoke(v, Get(v));
        }
    }

    /// <summary>Liest den aktuellen Wert einer Variable.</summary>
    public int Get(char variable)
    {
        lock (_lock)
            return _values[IndexOf(variable)];
    }

    /// <summary>
    /// Schreibt einen Wert in eine Variable.
    /// Der Wert wird auf den erlaubten Wertebereich [VarMin, VarMax] geklemmt.
    /// Feuert <see cref="VariableChanged"/> nur wenn der Wert sich tatsächlich geändert hat.
    /// </summary>
    public void Set(char variable, int value)
    {
        int clamped = Clamp(value);
        bool changed;
        lock (_lock)
        {
            int idx = IndexOf(variable);
            changed = _values[idx] != clamped;
            _values[idx] = clamped;
        }
        if (changed)
            VariableChanged?.Invoke(char.ToUpperInvariant(variable), clamped);
    }

    /// <summary>Gibt einen Snapshot aller 26 Variablenwerte zurück (A=0 … Z=25).</summary>
    public IReadOnlyDictionary<char, int> Snapshot()
    {
        lock (_lock)
        {
            var result = new Dictionary<char, int>(26);
            for (int i = 0; i < 26; i++)
                result[(char)('A' + i)] = _values[i];
            return result;
        }
    }

    // ── Hilfsmethoden ────────────────────────────────────────────────────────

    private static int IndexOf(char variable)
    {
        var upper = char.ToUpperInvariant(variable);
        if (upper < 'A' || upper > 'Z')
            throw new ArgumentOutOfRangeException(nameof(variable), $"Variable muss A–Z sein, war: {variable}");
        return upper - 'A';
    }

    private static int Clamp(int value) =>
        Math.Clamp(value, EngineState.VarMin, EngineState.VarMax);
}
