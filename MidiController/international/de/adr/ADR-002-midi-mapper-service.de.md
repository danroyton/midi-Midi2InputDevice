# ADR-002: MidiMapper als Windows-Dienst

| Feld | Wert |
|---|---|
| Status | **Akzeptiert** |
| Datum | 2025-05-30 |
| Entscheider | Entwicklungsteam |

## Kontext

Das Backend kann entweder als **normale Desktop-Applikation** (Tray-App) oder als **Windows-Dienst** betrieben werden. Beide Varianten haben unterschiedliche Implikationen für Latenz, Autostart, Sicherheit und die Fähigkeit, Tastatureingaben zu injizieren.

## Problem: SendInput und Windows-Dienste

Windows-Dienste laufen standardmäßig in **Session 0** (isoliert, nicht-interaktiv). `SendInput` und ähnliche UI-Automation-APIs funktionieren **nur in der interaktiven Benutzer-Session** (Session 1+).

Lösungsansätze:

| Ansatz | Aufwand | Zuverlässigkeit |
|---|---|---|
| Dienst + Tray-Helper-Prozess in Session 1 (via `CreateProcessAsUser`) | Hoch | Hoch |
| Dienst mit `AllowInteractToDesktop` (veraltet, Sicherheitsrisiko) | Niedrig | Niedrig |
| Kein Dienst; normaler Prozess mit Autostart via Task Scheduler (interaktiv) | Niedrig | Mittel |
| Dienst injiziert in interaktive Session über Named Pipe zu Tray-Prozess | Mittel | Hoch |

## Optionen

### Option A: Kein Dienst – Autostart via Task Scheduler
- Der Backend-Prozess startet beim Login des Benutzers.
- Läuft in der Benutzer-Session → `SendInput` funktioniert direkt.
- **Pro:** Einfach, kein Session-0-Problem.
- **Contra:** Kein Betrieb ohne angemeldeten Benutzer.
- **Contra:** Kein automatischer Neustart bei Absturz ohne zusätzliche Konfiguration.

### Option B: Windows-Dienst + Named Pipe zu Tray-Agent
- Dienst verwaltet MIDI-Input, Mapping-Engine, API.
- Minimaler Tray-Agent in Session 1 empfängt Injection-Befehle über Named Pipe und ruft `SendInput` auf.
- **Pro:** Autostart, läuft ohne Login.
- **Pro:** Klare Trennung von Dienst-Logik und UI-Injection.
- **Contra:** Zwei Prozesse, komplexere Kommunikation.

### Option C: Windows-Dienst, direkt in interaktive Session
- Dienst ermittelt aktive Session via `WTSGetActiveConsoleSessionId`.
- Startet den Injection-Helfer via `CreateProcessAsUser`.
- **Pro:** Vollautomatisch, kein manueller Tray-Agent.
- **Contra:** Erhöhter Implementierungsaufwand, elevated Rechte erforderlich.

## Entscheidung

**Zweistufiger Ansatz:**
1. **Phase 1:** Option A (kein Dienst, Autostart via Task Scheduler). Schnelle Umsetzbarkeit, kein Session-Problem.
2. **Phase 2 (optional):** Option B – Windows-Dienst + Tray-Agent, wenn Betrieb ohne angemeldeten Benutzer gefordert wird.

Die Architektur wird von Anfang an so gestaltet, dass die Injection-Logik hinter einem Interface (`IInputInjector`) gekapselt ist und ohne Änderung der restlichen Logik durch einen Named-Pipe-Proxy ersetzt werden kann.

## Begründung

Für den primären Anwendungsfall (Gaming/Produktivität am eigenen PC) ist ein Dienst-Betrieb ohne angemeldeten Benutzer nicht erforderlich. Der Mehraufwand von Option B lohnt sich erst, wenn dieser Use-Case explizit gefordert wird.

## Konsequenzen

- `IInputInjector` Interface wird definiert.
- Phase-1-Implementierung: direktes `SendInput` im Backend-Prozess.
- `BackgroundService.ExecuteAsync` wird mit `ThreadPriority.Highest` auf einem dedizierten Thread ausgeführt.
- Task-Scheduler-Aufgabe wird im Installer/Setup-Skript angelegt.
