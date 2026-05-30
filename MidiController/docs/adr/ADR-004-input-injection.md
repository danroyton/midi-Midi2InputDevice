# ADR-004: Methode zur Injektion von Tastatur- und Maus-Eingaben

| Feld | Wert |
|---|---|
| Status | **Akzeptiert** |
| Datum | 2025-05-30 |
| Entscheider | Entwicklungsteam |

## Kontext

Das Backend muss Tastatur- und Maus-Events in das Betriebssystem injizieren. Es gibt mehrere Windows-APIs mit unterschiedlichen Eigenschaften bezüglich Latenz, Kompatibilität und Einschränkungen.

## Optionen

### Option A: `SendInput` (user32.dll)
- Offizielle Win32-API für synthetische Eingaben.
- Unterstützt Tastatur (`INPUT_KEYBOARD`) und Maus (`INPUT_MOUSE`).
- Batch-fähig: Mehrere Events in einem einzigen Aufruf.
- **Pro:** Niedrige Latenz, gut dokumentiert, keine zusätzlichen Treiber.
- **Contra:** Wird von einigen Anti-Cheat-Systemen blockiert.
- **Contra:** Funktioniert nicht aus Session 0 (Dienst-Context, siehe ADR-002).

### Option B: `keybd_event` / `mouse_event` (veraltet)
- Ältere Win32-APIs, intern auf `SendInput` umgeleitet (ab Windows NT).
- **Contra:** Veraltet, kein Mehrwert gegenüber Option A.

### Option C: Windows Input Simulation (InputSimulator NuGet)
- .NET-Wrapper um `SendInput`.
- **Pro:** Einfacher zu verwenden als direktes P/Invoke.
- **Contra:** Zusätzliche Abhängigkeit; dünne Abstraktion ohne Mehrwert.

### Option D: Virtual HID (ViGEm / HidHide)
- Virtueller HID-Gamepad/Tastatur-Treiber.
- **Pro:** Für Gamepad-Emulation ideal; von Anti-Cheat toleriert.
- **Contra:** Kerneltreiber-Installation erforderlich; für reine Tastatureingaben überdimensioniert.

## Entscheidung

**Option A (`SendInput` via P/Invoke)** mit einer eigenen `InputInjector`-Klasse hinter dem `IInputInjector`-Interface.

Eigenes P/Invoke statt einer Bibliothek (Option C), da der Umfang überschaubar ist und keine zusätzliche Abhängigkeit gerechtfertigt ist.

## Begründung

- `SendInput` ist die empfohlene und modernste Win32-API für synthetische Eingaben.
- Batch-Aufrufe reduzieren den P/Invoke-Overhead für Kombinationen und Wiederholungen.
- Das Interface `IInputInjector` erlaubt spätere Erweiterung (z.B. ViGEm für Gamepad).

## Implementierungsdetails

```csharp
// P/Invoke-Signatur (Auszug)
[DllImport("user32.dll", SetLastError = true)]
static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

// Sequenz für Key-Hold (Y > 0):
// 1. KeyDown
// 2. Thread.Sleep(Y)
// 3. KeyUp
// Sequenz für Wiederholungen (X > 1):
// Schritt 1–3 wird X-mal wiederholt, mit Z ms Pause zwischen den Wiederholungen
```

## Konsequenzen

- `IInputInjector` wird als Interface im Domain-Layer definiert.
- Die `SendInput`-Implementierung liegt im Infrastructure-Layer.
- Anti-Cheat-Kompatibilität ist **kein** Ziel dieser Version; ein separates Ticket kann Option D evaluieren.
- Für Hold-Dauer (Y > 0) wird `Thread.Sleep` auf dem Injection-Thread verwendet – dieser ist vom HTTP-Thread isoliert und blockiert keine API-Anfragen.
