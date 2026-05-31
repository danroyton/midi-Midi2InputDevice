# ADR-004: Method for Injecting Keyboard and Mouse Input

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2025-05-30 |
| Decision makers | Development team |

## Context

The backend must inject keyboard and mouse events into the operating system. Windows provides several APIs with different characteristics regarding latency, compatibility, and limitations.

## Options

### Option A: `SendInput` (user32.dll)
- Official Win32 API for synthetic input.
- Supports keyboard (`INPUT_KEYBOARD`) and mouse (`INPUT_MOUSE`).
- Batch-capable: multiple events in a single call.
- **Pro:** Low latency, well documented, no additional drivers required.
- **Con:** Blocked by some anti-cheat systems.
- **Con:** Does not work from Session 0 (service context, see ADR-002).

### Option B: `keybd_event` / `mouse_event` (deprecated)
- Older Win32 APIs, internally redirected to `SendInput` (since Windows NT).
- **Con:** Deprecated; no advantage over Option A.

### Option C: Windows Input Simulation (InputSimulator NuGet)
- .NET wrapper around `SendInput`.
- **Pro:** Easier to use than direct P/Invoke.
- **Con:** Additional dependency; thin abstraction with no real benefit.

### Option D: Virtual HID (ViGEm / HidHide)
- Virtual HID gamepad/keyboard driver.
- **Pro:** Ideal for gamepad emulation; tolerated by anti-cheat systems.
- **Con:** Kernel driver installation required; overkill for pure keyboard input.

## Decision

**Option A (`SendInput` via P/Invoke)** with a custom `InputInjector` class behind the `IInputInjector` interface.

Custom P/Invoke instead of a library (Option C), because the scope is manageable and no additional dependency is justified.

## Rationale

- `SendInput` is the recommended and most modern Win32 API for synthetic input.
- Batch calls reduce P/Invoke overhead for key combinations and repetitions.
- The `IInputInjector` interface allows future extension (e.g. ViGEm for gamepad).

## Implementation Details

```csharp
// P/Invoke signature (excerpt)
[DllImport("user32.dll", SetLastError = true)]
static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

// Sequence for key hold (Y > 0):
// 1. KeyDown
// 2. Thread.Sleep(Y)
// 3. KeyUp
// Sequence for repetitions (X > 1):
// Steps 1–3 repeated X times, with Z ms pause between repetitions
```

## Consequences

- `IInputInjector` is defined as an interface in the domain layer.
- The `SendInput` implementation resides in the infrastructure layer.
- Anti-cheat compatibility is **not** a goal of this version; a separate ticket can evaluate Option D.
- For key hold duration (Y > 0), `Thread.Sleep` is used on the injection thread — this thread is isolated from the HTTP thread and does not block API requests.
