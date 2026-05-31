# 解决方案设计 – MidiController

## 概述

MidiController 从物理设备接收 MIDI 事件，评估映射规则，并将键盘/鼠标输入注入 Windows 操作系统。后端作为 ASP.NET Core Worker 服务运行（可作为 Windows 服务运行），并为 WPF 前端提供 REST API 和 SignalR Hub。

---

## 项目结构

```
Midi2InputDevice.slnx
│
├── MidiController/                         ← 文档项目（仅 docs/）
│   └── docs/
│       ├── ARCHITECTURE.md
│       ├── SPEC_BACKEND.md
│       ├── SPEC_FRONTEND.md
│       ├── solutiondesign.md               ← 本文件
│       └── adr/
│
├── MidiController.Domain/                  ← 类库（.NET 10）
├── MidiController.Engine/                  ← 类库（.NET 10）
├── MidiController.Infrastructure/          ← 类库（.NET 10，Windows）
├── MidiController.Host/                    ← ASP.NET Core Worker 服务（.NET 10）
│
├── MidiController.Frontend/                ← WPF 前端（.NET 10）
│
├── MidiController.Engine.Tests/            ← xUnit（.NET 10）
└── MidiController.Infrastructure.Tests/   ← xUnit（.NET 10）
```

---

## 项目 1 – `MidiController.Domain`

**用途：** 共享类型（record、枚举、接口）。不依赖解决方案中的其他项目。

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
│   └── EngineState.cs            // 常量 VarMin/VarMax，别名字典 A→ActiveListen 等
└── Interfaces/
	├── IMappingEngine.cs
	├── IInputInjector.cs
	├── IConfigStore.cs
	├── ITemplateStore.cs
	└── IMidiDeviceManager.cs
```

**依赖：** 无

---

## 项目 2 – `MidiController.Engine`

**用途：** 纯映射逻辑，完全可在无硬件或操作系统调用的情况下测试。

```
MidiController.Engine/
├── MappingEngine.cs              // IMappingEngine: ProcessEvent(MidiEvent)
├── MappingWorker.cs              // BackgroundService: 消费 Channel<MidiEvent>
├── Pipeline/
│   ├── DeltaTracker.cs           // 记录每个设备+事件类型+通道+Data1 的上次 Data1/Data2 值
│   └── ComputedValueContext.cs   // 计算 DD1PosAbs, DD1NegAbs, DD1Pos, DD1Neg, …
├── Evaluation/
│   ├── GateEvaluator.cs          // 变量 A：通过 / 暂停 / 阻止
│   ├── ConditionEvaluator.cs     // EvaluateBlock(ConditionBlock) → bool（OR 逻辑）
│   └── ValueResolver.cs          // Resolve(ValueSource, fixedValue, ctx) → int
├── Execution/
│   ├── TriggerExecutor.cs        // ExecuteTrigger: 全局预 → 条件 → 动作 → 全局后
│   ├── ActionExecutor.cs         // ExecuteAction: 解析 XYZ → SendInput → 状态赋值
│   └── ElseExecutor.cs           // ExecuteElseConfig(TriggerConfig)
└── State/
	└── VariableStore.cs          // 线程安全的 A–Z 获取/设置/重置
```

**依赖：** `MidiController.Domain`

---

## 项目 3 – `MidiController.Infrastructure`

**用途：** 领域接口的 Windows 特定实现。

```
MidiController.Infrastructure/
├── Midi/
│   ├── MidiInputService.cs       // BackgroundService，NAudio MidiIn，重连逻辑
│   └── VirtualMidiPortService.cs // loopMIDI COM / Windows MIDI Services API
├── Input/
│   ├── VirtualKeyMapper.cs       // 按键名称 → Windows VK 代码
│   └── WindowsInputInjector.cs   // IInputInjector 通过 P/Invoke SendInput
├── Config/
│   └── JsonConfigStore.cs        // IConfigStore + ITemplateStore：读写 JSON
└── Interop/
	└── NativeMethods.cs          // P/Invoke：SendInput, INPUT, KEYBDINPUT 结构
```

**依赖：** `MidiController.Domain`

---

## 项目 4 – `MidiController.Host`

**用途：** ASP.NET Core 主机、DI 根、REST 控制器、SignalR Hub。

```
MidiController.Host/
├── Program.cs                            // 主机构建器，UseWindowsService()，DI 配置
├── Controllers/
│   ├── DevicesController.cs
│   ├── ProfilesController.cs
│   ├── TriggersController.cs
│   ├── StatusController.cs
│   └── TemplatesController.cs
├── Hubs/
│   ├── MidiLogHub.cs                     // SignalR：MidiEventReceived(MidiEvent)
│   └── StatusHub.cs                      // SignalR：VariableChanged(string name, int value)
├── BackgroundServices/
│   └── PipelineBridgeService.cs          // 连接 MidiInput 通道与 MappingWorker
└── DI/
	└── ServiceCollectionExtensions.cs    // AddMidiEngine(), AddMidiInfrastructure()
```

**依赖：** `MidiController.Domain`、`MidiController.Engine`、`MidiController.Infrastructure`

---

## 依赖关系图

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

## 实现顺序

| 步骤 | 项目 | 内容 |
|---|---|---|
| 1 | `Domain` | 枚举、record、接口 |
| 2 | `Engine` | VariableStore、DeltaTracker、ValueResolver、ConditionEvaluator + 单元测试 |
| 3 | `Engine` | TriggerExecutor、ActionExecutor、MappingEngine + 测试 |
| 4 | `Infrastructure` | Win32InputInjector、JsonConfigStore、JsonTemplateStore |
| 5 | `Infrastructure` | MidiInputService（NAudio） |
| 6 | `Host` | Program.cs、控制器、Hub、DI 连接 |

---

## 关键设计决策

| 决策 | 原因 |
|---|---|
| 使用 `System.Threading.Channels` 替代 ConcurrentQueue | 背压支持，热路径中无内存分配 |
| 日志记录使用独立通道 | 不阻塞映射热路径 |
| `DD1PosAbs` / `DD1NegAbs` 作为计算只读值 | 无需 ELSE 分支即可编码旋转编码器的正/负方向 |
| 变量 A 作为激活门控（初始值为 2） | 安全启动：在明确激活前不产生意外键盘输入 |
| 所有领域类型使用 record | 不可变性、结构相等性、易于序列化 |
