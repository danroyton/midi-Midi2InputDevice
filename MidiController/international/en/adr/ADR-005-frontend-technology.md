# ADR-005: Frontend Technology

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2025-05-30 |
| Updated | 2025-05-30 (Avalonia UI evaluation added) |
| Decision makers | Development team |

## Context

The frontend is a Windows configuration application. It needs to display raw MIDI data in real time, render complex trigger configuration forms, and communicate with the backend via REST and WebSocket.

## Options

### Option A: WPF (.NET 10)
- Mature Windows GUI framework.
- MVVM pattern with `CommunityToolkit.Mvvm`.
- **Pro:** Native Windows look-and-feel; no browser runtime required.
- **Pro:** Direct access to Windows APIs if needed.
- **Pro:** Fully supported on .NET 10.
- **Con:** Windows only; not cross-platform.

### Option B: WinUI 3 / Windows App SDK
- More modern UI platform than WPF.
- **Pro:** Modern Fluent Design.
- **Con:** Higher entry barrier; fewer community resources than WPF.
- **Con:** More complex deployment (MSIX package recommended).

### Option C: .NET MAUI
- Cross-platform (Windows, macOS, iOS, Android).
- **Pro:** Future-proof; one codebase for all platforms.
- **Con:** Windows target internally uses WinUI 3; not yet as stable as WPF.
- **Con:** Over-engineered for a pure Windows configuration app.

### Option E: Avalonia UI (.NET 10)
- Cross-platform GUI framework for .NET; renders entirely via Skia (SkiaSharp) — no native OS widgets.
- Supports `CommunityToolkit.Mvvm` and ReactiveUI; actively developed; stable API since version 11.
- **Pro:** Cross-platform (Windows, macOS, Linux) without WinRT or MSIX dependency.
- **Pro:** Modern, highly stylable UI; dark mode and custom themes easy to implement.
- **Pro:** No Electron/browser overhead; lean binary footprint.
- **Pro:** Full .NET 10 support; XAML-based — WPF knowledge transfers.
- **Pro:** `Avalonia.Controls.DataGrid` and `ItemsRepeater` well suited for the MIDI log table and trigger lists.
- **Con:** No native Windows look-and-feel (Skia rendering); looks different from WPF/WinUI on Windows.
- **Con:** Smaller community and fewer third-party controls than WPF.
- **Con:** Designer tooling in Visual Studio weaker than WPF (no official XAML designer; JetBrains Rider is better supported).
- **Con:** Native Windows API access (e.g. for tray integration) requires platform-specific code.

**Assessment for this project:**  
Since the target application is primarily Windows-only and the backend is Win32-specific (`SendInput`, MIDI WinMM), cross-platform in the frontend brings no immediate benefit. For a pure configuration tool, Avalonia is a valid WPF alternative if modern styling or a future macOS/Linux port is a priority. The missing Visual Studio designer noticeably increases UI development effort.

### Option D: Electron / Web frontend (React/Vue)
- UI in a browser window.
- **Pro:** Rich ecosystem of UI components.
- **Con:** Significantly heavier (Node.js, Chromium).
- **Con:** No native Windows feel.

## Decision

**Option A (WPF, .NET 10)** — with an explicit evaluation against Option E (Avalonia UI).

## Rationale

- The target platform is exclusively Windows; cross-platform brings no benefit.
- WPF is mature, well documented, and fully supported in the .NET 10 ecosystem.
- `CommunityToolkit.Mvvm` reduces boilerplate to a minimum.
- The team has existing .NET knowledge — no JavaScript/TypeScript context required.
- **Compared to Avalonia:** WPF provides the full Visual Studio XAML designer, a larger community, and native Windows rendering. Since the backend remains Win32-specific (SendInput, MIDI WinMM), cross-platform is not an argument. Should the project expand to macOS/Linux, switching to Avalonia as a drop-in replacement would be feasible due to XAML similarity.

## Consequences

- A new WPF project `KarentaMidi2DeviceInput.UI` is added to the solution.
- NuGet dependencies: `CommunityToolkit.Mvvm`, `Microsoft.AspNetCore.SignalR.Client`, Polly.
- The frontend is not cross-platform; a later switch to Avalonia UI is possible since the MVVM ViewModel layer remains framework-independent (only the View layer would need to be ported).
