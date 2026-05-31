# Solution Design – MidiController

## Overview

The MidiController receives MIDI events from physical devices, evaluates mapping rules, and injects keyboard/mouse inputs into the Windows operating system. The backend runs as an ASP.NET Core Worker Service (can be run as a Windows Service) and provides a REST API and SignalR hubs for the WPF frontend.

---

## Project Structure

```
Midi2InputDevice.slnx
│
├── MidiController/                         ← Documentation project (docs/ only)
│   └── docs/
│       ├── ARCHITECTURE.md
│       ├── SPEC_BACKEND.md
│       ├── SPEC_FRONTEND.md
│       ├── solutiondesign.md               ← this file
│       └── adr/
│
├── MidiController.Domain/                  ← Class Library (.NET 10)
├── MidiController.Engine/                  ← Class Library (.NET 10)
├── MidiController.Infrastructure/          ← Class Library (.NET 10, Windows)
├── MidiController.Host/                    ← ASP.NET Core Worker Service (.NET 10)
│
├── MidiController.Frontend/                ← WPF Frontend (.NET 10)
│
├── MidiController.Engine.Tests/            ← xUnit (.NET 10)
└── MidiController.Infrastructure.Tests/    ← xUnit (.NET 10)
```

---

## Project 1 – `MidiController.Domain`

**Purpose:** Shared types (records, enums, interfaces). No dependencies on other solution projects.

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
│   └── EngineState.cs            // Constants VarMin/VarMax, alias dictionary A→ActiveListen etc.
└── Interfaces/
	├── IMappingEngine.cs
	├── IInputInjector.cs
	├── IConfigStore.cs
	├── ITemplateStore.cs
	└── IMidiDeviceManager.cs
```

**Dependencies:** none

---

## Project 2 – `MidiController.Engine`

**Purpose:** Pure mapping logic, fully testable without hardware or OS calls.

```
MidiController.Engine/
├── MappingEngine.cs              // IMappingEngine: ProcessEvent(MidiEvent)
├── MappingWorker.cs              // BackgroundService: consume Channel<MidiEvent>
├── Pipeline/
│   ├── DeltaTracker.cs           // Remember last Data1/Data2 value per device+EventType+channel+Data1
│   └── ComputedValueContext.cs   // Calculate DD1PosAbs, DD1NegAbs, DD1Pos, DD1Neg, …
├── Evaluation/
│   ├── GateEvaluator.cs          // Variable A: Pass / Paused / Blocked
│   ├── ConditionEvaluator.cs     // EvaluateBlock(ConditionBlock) → bool (OR logic)
│   └── ValueResolver.cs          // Resolve(ValueSource, fixedValue, ctx) → int
├── Execution/
│   ├── TriggerExecutor.cs        // ExecuteTrigger: GlobalPre → Conditions → Actions → GlobalPost
│   ├── ActionExecutor.cs         // ExecuteAction: resolve XYZ → SendInput → StateAssignments
│   └── ElseExecutor.cs           // ExecuteElseConfig(TriggerConfig)
└── State/
	└── VariableStore.cs          // thread-safe A–Z Get/Set/Reset
```

**Dependencies:** `MidiController.Domain`

---

## Project 3 – `MidiController.Infrastructure`

**Purpose:** Windows-specific implementations of the domain interfaces.

```
MidiController.Infrastructure/
├── Midi/
│   ├── MidiInputService.cs       // BackgroundService, NAudio MidiIn, reconnect logic
│   └── VirtualMidiPortService.cs // loopMIDI COM / Windows MIDI Services API
├── Input/
│   ├── VirtualKeyMapper.cs       // Key names → Windows VK codes
│   └── WindowsInputInjector.cs   // IInputInjector via P/Invoke SendInput
├── Config/
│   └── JsonConfigStore.cs        // IConfigStore + ITemplateStore: read/write JSON
└── Interop/
	└── NativeMethods.cs          // P/Invoke: SendInput, INPUT, KEYBDINPUT structs
```

**Dependencies:** `MidiController.Domain`

---

## Project 4 – `MidiController.Host`

**Purpose:** ASP.NET Core host, DI root, REST controllers, SignalR hubs.

```
MidiController.Host/
├── Program.cs                            // Host builder, UseWindowsService(), DI setup
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
│   └── PipelineBridgeService.cs          // Connects MidiInput channel with MappingWorker
└── DI/
	└── ServiceCollectionExtensions.cs    // AddMidiEngine(), AddMidiInfrastructure()
```

**Dependencies:** `MidiController.Domain`, `MidiController.Engine`, `MidiController.Infrastructure`

---

## Dependency Diagram

```
MidiController.Domain   ◄──  MidiController.Engine
					◄──  MidiController.Infrastructure
					◄──  MidiController.Host
MidiController.Engine   ◄──  MidiController.Host
MidiController.Infrastructure ◄── MidiController.Host

MidiController.Engine        ◄── MidiController.Engine.Tests
MidiController.Infrastructure ◄── MidiController.Infrastructure.Tests
```

---

## Implementation Order

| Step | Project | Content |
|---|---|---|
| 1 | `Domain` | Enums, records, interfaces |
| 2 | `Engine` | VariableStore, DeltaTracker, ValueResolver, ConditionEvaluator + unit tests |
| 3 | `Engine` | TriggerExecutor, ActionExecutor, MappingEngine + tests |
| 4 | `Infrastructure` | Win32InputInjector, JsonConfigStore, JsonTemplateStore |
| 5 | `Infrastructure` | MidiInputService (NAudio) |
| 6 | `Host` | Program.cs, controllers, hubs, DI wiring |

---

## Key Design Decisions

| Decision | Rationale |
|---|---|
| `System.Threading.Channels` instead of ConcurrentQueue | Backpressure, no alloc in the hot path |
| Logging on a separate channel | Does not block the mapping hot path |
| `DD1PosAbs` / `DD1NegAbs` as computed read-only values | Positive/negative encoder direction encodable without an ELSE branch |
| Variable A as activation gate (initial value 2) | Safe start: no unintended keyboard input before explicit activation |
| Records for all domain types | Immutability, structural equality, easy serialization |
