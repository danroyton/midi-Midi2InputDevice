# Solution Design – MidiController

## Überblick

Der MidiController empfängt MIDI-Events von physikalischen Geräten, wertet Mapping-Regeln aus und injiziert Tastatur-/Mauseingaben in das Windows-Betriebssystem. Das Backend läuft als ASP.NET Core Worker Service (kann als Windows-Dienst betrieben werden) und stellt eine REST-API sowie SignalR-Hubs für das WPF-Frontend bereit.

---

## Projektstruktur

```
MidiController.sln
│
├── MidiController/                         ← Dokumentations-Projekt (nur docs/)
│   └── docs/
│       ├── ARCHITECTURE.md
│       ├── SPEC_BACKEND.md
│       ├── SPEC_FRONTEND.md
│       ├── solutiondesign.md               ← diese Datei
│       └── adr/
│
├── MidiController.Domain/                  ← Class Library (.NET 10)
├── MidiController.Engine/                  ← Class Library (.NET 10)
├── Midicontroller.Infrastructure/          ← Class Library (.NET 10, Windows)
├── MidiController.Host/                    ← ASP.NET Core Worker Service (.NET 10)
│
├── MidiControllerFrontend/                 ← WPF-Frontend (.NET 10)
│
├── MidiController.Engine.Tests/            ← xUnit (.NET 10)
└── MidiController.Infrastructure.Tests/    ← xUnit (.NET 10)
```

---

## Projekt 1 – `MidiController.Domain`

**Zweck:** Gemeinsame Typen (Records, Enums, Interfaces). Keine Abhängigkeiten zu anderen Solution-Projekten.

```
MidiController.Domain/
├── Enums/
│   ├── MidiEventType.cs          // NoteOn, NoteOff, ControlChange, ProgramChange, …
│   └── ValueSource.cs            // Fixed, MidiData1, DD1PosAbs, VariableA…Z, …
├── Models/
│   ├── MidiEvent.cs              // record MidiEvent(DeviceId, Type, Channel, Data1, Data2, TimestampUs)
│   ├── ConditionBlock.cs         // record Condition + record ConditionBlock
│   ├── ActionBlock.cs            // record StateAssignment + record ActionBlock
│   ├── Trigger.cs                // record Trigger + record TriggerConfig
│   └── Profile.cs                // record Profile + record DeviceMapping
├── State/
│   └── EngineState.cs            // Konstanten VarMin/VarMax, Alias-Dictionary A→ActiveListen usw.
└── Interfaces/
	├── IMappingEngine.cs
	├── IInputInjector.cs
	├── IConfigStore.cs
	├── ITemplateStore.cs
	└── IMidiDeviceManager.cs
```

**Abhängigkeiten:** keine

---

## Projekt 2 – `MidiController.Engine`

**Zweck:** Reine Mapping-Logik, vollständig testbar ohne Hardware oder OS-Aufruf.

```
MidiController.Engine/
├── MappingEngine.cs              // IMappingEngine: ProcessEvent(MidiEvent)
├── MappingWorker.cs              // BackgroundService: Channel<MidiEvent> konsumieren
├── Pipeline/
│   ├── DeltaTracker.cs           // Letzten Data1/Data2-Wert pro Gerät+EventType+Kanal+Data1 merken
│   └── ComputedValueContext.cs   // DD1PosAbs, DD1NegAbs, DD1Pos, DD1Neg, … berechnen
├── Evaluation/
│   ├── GateEvaluator.cs          // Variable A: Pass / Paused / Blocked
│   ├── ConditionEvaluator.cs     // EvaluateBlock(ConditionBlock) → bool (OR-Logik)
│   └── ValueResolver.cs          // Resolve(ValueSource, fixedValue, ctx) → int
├── Execution/
│   ├── TriggerExecutor.cs        // ExecuteTrigger: GlobalPre → Conditions → Actions → GlobalPost
│   ├── ActionExecutor.cs         // ExecuteAction: XYZ auflösen → SendInput → StateAssignments
│   └── ElseExecutor.cs           // ExecuteElseConfig(TriggerConfig)
└── State/
	└── VariableStore.cs          // thread-safe A–Z Get/Set/Reset
```

**Abhängigkeiten:** `MidiController.Domain`

---

## Projekt 3 – `Midicontroller.Infrastructure`

**Zweck:** Windows-spezifische Implementierungen der Domain-Interfaces.

```
Midicontroller.Infrastructure/
├── Midi/
│   ├── MidiInputService.cs       // BackgroundService, NAudio MidiIn, Reconnect-Logik
│   └── VirtualMidiPortService.cs // loopMIDI COM / Windows MIDI Services API
├── Input/
│   └── Win32InputInjector.cs     // IInputInjector via P/Invoke SendInput
├── Persistence/
│   ├── JsonConfigStore.cs        // IConfigStore: Profile als JSON lesen/schreiben
│   └── JsonTemplateStore.cs      // ITemplateStore: Templates als JSON lesen/schreiben
└── Interop/
	└── NativeMethods.cs          // P/Invoke: SendInput, INPUT, KEYBDINPUT structs
```

**Abhängigkeiten:** `MidiController.Domain`

---

## Projekt 4 – `MidiController.Host`

**Zweck:** ASP.NET Core-Host, DI-Wurzel, REST-Controller, SignalR-Hubs.

```
MidiController.Host/
├── Program.cs                            // Host-Builder, UseWindowsService(), DI-Setup
├── Controllers/
│   ├── DevicesController.cs
│   ├── ProfilesController.cs
│   ├── TriggersController.cs
│   ├── StatusController.cs
│   └── TemplatesController.cs
├── Hubs/
│   ├── MidiLogHub.cs                     // SignalR: MidiEventReceived(MidiEvent)
│   └── StatusHub.cs                      // SignalR: VariableChanged(string name, int value)
├── BackgroundServices/
│   └── PipelineBridgeService.cs          // Verbindet MidiInput-Channel mit MappingWorker
└── DI/
	└── ServiceCollectionExtensions.cs    // AddMidiEngine(), AddMidiInfrastructure()
```

**Abhängigkeiten:** `MidiController.Domain`, `MidiController.Engine`, `Midicontroller.Infrastructure`

---

## Abhängigkeitsdiagramm

```
MidiController.Domain   ◄──  MidiController.Engine
						◄──  Midicontroller.Infrastructure
						◄──  MidiController.Host
MidiController.Engine   ◄──  MidiController.Host
Midicontroller.Infrastructure ◄── MidiController.Host

MidiController.Engine        ◄── MidiController.Engine.Tests
Midicontroller.Infrastructure ◄── MidiController.Infrastructure.Tests
```

---

## Implementierungsreihenfolge

| Schritt | Projekt | Inhalt |
|---|---|---|
| 1 | `Domain` | Enums, Records, Interfaces |
| 2 | `Engine` | VariableStore, DeltaTracker, ValueResolver, ConditionEvaluator + Unit Tests |
| 3 | `Engine` | TriggerExecutor, ActionExecutor, MappingEngine + Tests |
| 4 | `Infrastructure` | Win32InputInjector, JsonConfigStore, JsonTemplateStore |
| 5 | `Infrastructure` | MidiInputService (NAudio) |
| 6 | `Host` | Program.cs, Controller, Hubs, DI-Verdrahtung |

---

## Wichtige Designentscheidungen

| Entscheidung | Begründung |
|---|---|
| `System.Threading.Channels` statt ConcurrentQueue | Backpressure, kein Alloc im Hot-Path |
| Logging auf separatem Channel | Blockiert den Mapping-Hot-Path nicht |
| `DD1PosAbs` / `DD1NegAbs` als berechnete Lesewerte | Positive/negative Encoder-Drehrichtung ohne ELSE-Zweig codierbar |
| Variable A als Aktivierungs-Gate (Initialwert 2) | Sicherer Start: keine ungewollte Tastatureingabe vor expliziter Aktivierung |
| Records für alle Domäntypen | Immutabilität, strukturelle Gleichheit, einfaches Serialisieren |
