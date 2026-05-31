# 后端规范 – MidiController

## 1. 概述

后端是一个 **ASP.NET Core Worker 服务（.NET 10）**，可同时作为 Windows 服务运行。它负责：

- 从物理设备接收 MIDI 事件
- 管理虚拟 MIDI 端口
- 评估映射规则
- 将输入事件（键盘、鼠标）注入操作系统
- 为前端提供 REST API 和 WebSocket 流

---

## 2. 服务与组件

### 2.1 MidiInputService（`BackgroundService`）

| 属性 | 说明 |
|---|---|
| 库 | NAudio（`MidiIn`）或 RtMidi.NET |
| 线程 | 每个物理设备一个后台线程 |
| 输出 | 将 `MidiEvent` 对象写入 `System.Threading.Channels.Channel<MidiEvent>` |
| 优先级 | `ThreadPriority.Highest` |

**MidiEvent 数据模型：**

```csharp
public record MidiEvent(
	string DeviceId,
	MidiEventType Type,   // NoteOn, NoteOff, ControlChange, ProgramChange, …
	int Channel,
	int Data1,            // 音符 / CC 编号
	int Data2,            // 力度 / CC 值
	long TimestampUs      // 自进程启动以来的微秒数
);
```

### 2.2 VirtualMidiPortService

- 通过 **loopMIDI**（COM 自动化）或 **Windows MIDI Services API**（Windows 11 24H2 起）创建逻辑（虚拟）MIDI 端口。
- 一个物理设备可镜像到 1–N 个虚拟端口。
- 配置位于 `profiles/{profile}.json` 的 `virtualPorts[]` 下。

### 2.3 事件管道

```
MidiInputService
	  │
	  ▼  Channel<MidiEvent>（有界，容量=512）
 MappingWorker  ──────────────────────────────────────────────────▶ InputInjector
	  │
	  └──── LogBroadcastChannel ──▶ SignalR Hub ──▶ 前端
```

- 使用 `System.Threading.Channels`（热路径中无锁、无内存分配）。
- `MappingWorker` 在具有提升优先级的专用线程上运行。
- 日志记录在**独立**通道上进行（较低优先级），避免阻塞热路径。

### 2.4 MappingEngine

#### 2.4.1 状态变量

所有持久变量（**A–Z**）的值范围为 **-127 … +127**。MIDI 值 Data1/Data2 限制在 0–127，但差值和直接赋值可以为负数。

| 变量 | 别名 | 初始值 | 保留 | 说明 |
|---|---|---|---|---|
| `A` | `ActiveListen` | **2** | **是** | **激活门控**（见 2.4.2） |
| `B` – `U` | – | 0 | 否 | 可自由使用的状态变量 |
| `X` | `Repeat` | 1 | 否 | 按键重复次数（默认：1） |
| `Y` | `KeyDuration` | 0 | 否 | 按键保持时长（ms，默认：0 = 短按） |
| `Z` | `Pause` | 0 | 否 | 按键后暂停时长（ms，默认：0） |

此外，每次事件后后端提供以下**计算只读值**（只读，可作为条件和 X/Y/Z 赋值的来源）：

| 标识符 | 值范围 | 说明 |
|---|---|---|
| `DeltaData2` | -127 – 127 | 与同类型+通道+Data1 上次事件的 Data2 差值（首次出现时为 0） |
| `DD2Positive` | 0 – 127 | 当 Δ > 0 时为 DeltaData2 的绝对值，否则为 0 |
| `DD2Negative` | 0 – 127 | 当 Δ < 0 时为 DeltaData2 的绝对值，否则为 0 |

> **追踪规则：** 对于每个三元键 `(Type, Channel, Data1)`，存储最后看到的 `Data2` 值。首次出现时若无记录，则使用当前 `Data2` 值作为前驱值 → Delta = 0。这可防止具有固定偏移值的开关产生不期望的跳变值。

##### 变量 A – 激活门控

| A 的值 | 行为 |
|---|---|
| **0** | 正常：所有触发器被完整评估。 |
| **1** | 暂停：跳过条件块。仅执行将 `A` 设为 `0` 的动作。充当软暂停/断路器。 |
| **2**（默认） | 阻止：MIDI 事件完全不被处理。仅通过 API 调用或前端才能返回 `A=0`。 |

#### 2.4.2 触发器评估（每次传入 MidiEvent）

```
传入 MIDI 事件
	   │
	   ▼
┌──────────────────────────────────────────┐
│ 门控检查：变量 A                         │
│  A==2 → 丢弃事件（返回）                 │
│  A==1 → 仅检查设置 A 的动作             │
│  A==0 → 继续                            │
└──────────────────┬───────────────────────┘
				   │
				   ▼
┌──────────────────────────────────────────┐
│ 步骤 0：设置全局状态                     │ ← 可选，在条件块之前执行
│   例如 B=1（"正在运行"）               │
└──────────────────┬───────────────────────┘
				   │
				   ▼
┌──────────────────────────────────────────┐  ┐
│ 条件块 1                                 │  │
│   OR 条件：至少一个为真                 │  │
│   例如 [B==1 OR C>5]                   │  │
├──────────────────┬───────────────────────┤  │ 块之间为
│ 真               │ 假                    │  │ AND 链接
│      ▼           │     ▼ 有 ELSE 配置？  │  │
│ 条件块 2         │     是 → ELSE 分支    │  │
│   …              │     否 → 返回         │  │
└──────────────────┴───────────────────────┘  ┘
	   │（所有块均为真）
	   ▼
┌──────────────────────────────────────────┐
│ 动作（1..n，顺序执行）                   │
│  每个动作：                             │
│   1. 解析 X/Y/Z 来源                   │
│   2. 发送按键（如已设置）              │
│   3. 此动作的状态赋值                  │
│  所有动作完成后：                       │
│   4. 全局后赋值                        │
└──────────────────────────────────────────┘
```

#### 2.4.3 数据模型

```csharp
public const int VarMin = -127;
public const int VarMax = 127;

public enum ValueSource
{
	Fixed,
	MidiData1,
	MidiData2,
	DeltaData2,
	DD2Positive,
	DD2Negative,
	VariableA, VariableB, /* … */ VariableZ
}

public record Condition(
	ValueSource Left,
	string      Op,        // "==", "!=", "<", ">", "<=", ">="
	ValueSource RightSource,
	int         RightFixed
);

public record ConditionBlock(
	string?    TemplateName,
	Condition[] Conditions
);

public record ActionBlock(
	string?           TemplateName,
	string[]          KeyCombination,
	ValueSource       XSource, int XFixed,
	ValueSource       YSource, int YFixed,
	ValueSource       ZSource, int ZFixed,
	StateAssignment[] StateAssignments
);

public enum TriggerMatchMode
{
	Variable,
	Data2,
	DeltaData2,
	DD2Positive,
	DD2Negative,
}

public record StateAssignment(char Variable, ValueSource Source, int FixedValue);

public record Trigger(
	string           TriggerId,
	string           DeviceId,
	MidiEventType    EventType,
	int              Channel,
	int?             Data1Filter,
	TriggerMatchMode MatchMode,
	int              MatchValue,
	StateAssignment[] GlobalPreAssignments,
	ConditionBlock[] ConditionBlocks,
	ActionBlock[]    Actions,
	StateAssignment[] GlobalPostAssignments,
	TriggerConfig?   ElseConfig
);

public record TriggerConfig(
	ConditionBlock[] ConditionBlocks,
	ActionBlock[]    Actions,
	StateAssignment[] GlobalPostAssignments
);
```

#### 2.4.4 模板

条件块和动作块可以命名保存，并在多个触发器中复用。

存储位置：`%ProgramData%\MidiController\templates\`

REST API（位于 `/api/v1/templates`）：

| 方法 | 路径 | 说明 |
|---|---|---|
| `GET` | `/templates` | 列出所有模板 |
| `GET` | `/templates/{name}` | 加载模板 |
| `POST` | `/templates` | 创建模板 |
| `PUT` | `/templates/{name}` | 覆盖模板 |
| `DELETE` | `/templates/{name}` | 删除模板 |

### 2.5 InputInjector

- 通过 **Win32 `SendInput`**（P/Invoke）实现。
- 支持：单键、组合键、按下+保持+释放序列。
- `X` = 重复次数，`Y` = 保持时长（ms），`Z` = 之后暂停（ms）。
- 线程优先级：`Highest`；注入路径上无 await。

### 2.6 配置存储

存储位置：`%ProgramData%\MidiController\profiles\`

---

## 3. REST API

基础 URL：`http://localhost:5173/api/v1`

### 3.1 设备

| 方法 | 路径 | 说明 |
|---|---|---|
| `GET` | `/devices` | 列出所有物理 MIDI 设备 |
| `GET` | `/devices/virtual` | 列出所有虚拟端口 |
| `POST` | `/devices/virtual` | 创建虚拟端口 |
| `DELETE` | `/devices/virtual/{id}` | 删除虚拟端口 |

### 3.2 配置文件

| 方法 | 路径 | 说明 |
|---|---|---|
| `GET` | `/profiles` | 列出所有配置文件 |
| `GET` | `/profiles/{id}` | 加载配置文件 |
| `POST` | `/profiles` | 创建配置文件 |
| `PUT` | `/profiles/{id}` | 保存配置文件 |
| `DELETE` | `/profiles/{id}` | 删除配置文件 |
| `POST` | `/profiles/{id}/activate` | 激活配置文件 |

### 3.3 触发器

| 方法 | 路径 | 说明 |
|---|---|---|
| `GET` | `/profiles/{id}/triggers` | 列出配置文件的触发器 |
| `POST` | `/profiles/{id}/triggers` | 创建触发器 |
| `PUT` | `/profiles/{id}/triggers/{tid}` | 更新触发器 |
| `DELETE` | `/profiles/{id}/triggers/{tid}` | 删除触发器 |

### 3.4 状态

| 方法 | 路径 | 说明 |
|---|---|---|
| `GET` | `/status` | 活动配置文件、连接状态、CPU |
| `GET` | `/status/variables` | A–Z 的当前值（含 X、Y、Z） |
| `PUT` | `/status/variables/{variable}` | 设置单个变量（值范围 -127…+127）；尤其是 `A` 用于启用/禁用处理 |

### 3.5 WebSocket / SignalR

| Hub | 路径 | 事件 |
|---|---|---|
| `MidiLogHub` | `/hubs/midilog` | `MidiEventReceived(MidiEvent)` |
| `StatusHub` | `/hubs/status` | `VariableChanged(string variableName, int value)` |

---

## 4. Windows 服务模式

- `Program.cs` 中使用 `UseWindowsService()`。
- 安装：`sc create MidiController binPath=...`
- 优点：自动启动，无需用户登录即可运行。
- **限制**：`SendInput` 仅在交互式会话中有效；服务需在 Session 1 中运行或通过 `WTSGetActiveConsoleSessionId` / `CreateProcessAsUser` 注入（见 ADR-002）。

---

## 5. 错误处理与日志记录

- `Microsoft.Extensions.Logging` 配合 Serilog Sink（文件 + 控制台）。
- 自动检测 MIDI 断开并重新连接端口。
- 触发器配置错误在加载时进行验证并记录为警告；配置文件的其余部分保持活动。
