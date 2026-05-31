# ADR-005: Wahl der Frontend-Technologie

| Feld | Wert |
|---|---|
| Status | **Akzeptiert** |
| Datum | 2025-05-30 |
| Aktualisiert | 2025-05-30 (Avalonia UI ergänzt) |
| Entscheider | Entwicklungsteam |

## Kontext

Das Frontend ist eine Konfigurationsapplikation für Windows. Es muss MIDI-Rohdaten in Echtzeit anzeigen, komplexe Formulare für Trigger-Konfigurationen rendern und mit dem Backend über REST und WebSocket kommunizieren.

## Optionen

### Option A: WPF (.NET 10)
- Etabliertes Windows-GUI-Framework.
- MVVM-Pattern mit `CommunityToolkit.Mvvm`.
- **Pro:** Natives Windows-Look-and-Feel; keine Browser-Runtime erforderlich.
- **Pro:** Direkter Zugriff auf Windows-APIs falls nötig.
- **Pro:** .NET 10 vollständig unterstützt.
- **Contra:** Nur Windows; kein Cross-Platform.

### Option B: WinUI 3 / Windows App SDK
- Modernere UI-Plattform als WPF.
- **Pro:** Modernes Fluent Design.
- **Contra:** Höhere Einstiegshürde; weniger Community-Ressourcen als WPF.
- **Contra:** Deployment komplexer (MSIX-Paket empfohlen).

### Option C: .NET MAUI
- Cross-Platform (Windows, macOS, iOS, Android).
- **Pro:** Zukunftssicher, eine Codebasis für alle Plattformen.
- **Contra:** Windows-Ziel nutzt intern WinUI 3; noch nicht so stabil wie WPF.
- **Contra:** Für eine reine Windows-Konfigurations-App überengineered.

### Option E: Avalonia UI (.NET 10)
- Cross-Platform-GUI-Framework auf Basis von .NET; rendert vollständig eigens via Skia (SkiaSharp) – kein nativer OS-Widget-Einsatz.
- Verbreitetes MVVM-Pattern; vollständige Unterstützung von `CommunityToolkit.Mvvm` und ReactiveUI.
- Aktiv entwickelt; seit Version 11 stabile API.
- **Pro:** Cross-Platform (Windows, macOS, Linux) ohne Abhängigkeit von WinRT oder MSIX.
- **Pro:** Modernes, flexibel stylbares UI; Dark-Mode und Custom-Themes einfach umsetzbar.
- **Pro:** Kein Electron/Browser-Overhead; schlanker Binary-Footprint.
- **Pro:** Vollständige .NET 10 Unterstützung; XAML-basiert – WPF-Wissen ist übertragbar.
- **Pro:** `Avalonia.Controls.DataGrid` und `ItemsRepeater` eignen sich gut für die MIDI-Log-Tabelle und Trigger-Listen.
- **Contra:** Kein natives Windows-Look-and-Feel (Skia-Rendering); sieht auf Windows anders aus als WPF/WinUI.
- **Contra:** Kleinere Community und weniger Drittanbieter-Controls als WPF.
- **Contra:** Designer-Tooling in Visual Studio schwächer als WPF (kein offizieller XAML-Designer; JetBrains Rider ist besser unterstützt).
- **Contra:** Nativer Windows-API-Zugriff (z.B. für zukünftige Tray-Integration) erfordert Platform-spezifischen Code.

**Bewertung für dieses Projekt:**  
Da die Zielanwendung primär Windows-only ist und das Backend Win32-spezifisch (`SendInput`, MIDI WinMM) ist, bringt Cross-Platform im Frontend keinen unmittelbaren Mehrwert. Für ein reines Konfigurations-Tool ist Avalonia eine valide Alternative zu WPF, wenn modernes Styling oder spätere macOS/Linux-Portierung priorisiert wird. Der fehlende Visual-Studio-Designer erhöht den Entwicklungsaufwand bei der UI-Erstellung merklich.

### Option D: Electron / Web-Frontend (React/Vue)
- UI im Browser-Fenster.
- **Pro:** Reichhaltiges Ökosystem für UI-Komponenten.
- **Contra:** Deutlich schwergewichtiger (Node.js, Chromium).
- **Contra:** Kein natives Windows-Feeling.

## Entscheidung

**Option A (WPF, .NET 10)** – mit expliziter Abwägung gegen Option E (Avalonia UI).

## Begründung

- Die Zielplattform ist ausschließlich Windows; Cross-Platform bringt keinen Mehrwert.
- WPF ist ausgereift, gut dokumentiert und im .NET 10 Ökosystem vollständig unterstützt.
- `CommunityToolkit.Mvvm` reduziert Boilerplate auf ein Minimum.
- Das Team hat vorhandenes .NET-Wissen – kein JavaScript/TypeScript-Kontext nötig.
- **Gegenüber Avalonia:** WPF bietet den vollständigen Visual-Studio-XAML-Designer, eine größere Community und natives Windows-Rendering. Da das Backend Win32-spezifisch bleibt (SendInput, MIDI WinMM), ist Cross-Platform kein Argument. Sollte das Projekt auf macOS/Linux ausgeweitet werden, wäre Avalonia als Drop-In-Ersatz durch die XAML-Ähnlichkeit mit vertretbarem Aufwand möglich.

## Konsequenzen

- Neues WPF-Projekt `KarentaMidi2DeviceInput.UI` in der Solution anlegen.
- NuGet-Abhängigkeiten: `CommunityToolkit.Mvvm`, `Microsoft.AspNetCore.SignalR.Client`, Polly.
- Das Frontend ist nicht cross-platform; ein späterer Wechsel zu Avalonia UI ist möglich, da das MVVM-ViewModel-Layer framework-unabhängig bleibt (nur View-Layer müsste portiert werden).
