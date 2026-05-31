# ADR-001: 选择 MIDI 输入库

| 字段 | 值 |
|---|---|
| 状态 | **已接受** |
| 日期 | 2025-05-30 |
| 决策者 | 开发团队 |

## 背景

后端需要在 Windows 下从物理设备接收 MIDI 事件。所选库对延迟和可维护性有直接影响。

## 选项

### 选项 A：NAudio（`NAudio.Midi.MidiIn`）
- **优点：** 使用广泛，.NET 原生，持续维护，可通过 NuGet 获取。
- **优点：** 直接绑定 `winmm.dll`，延迟低（约 1–2 ms）。
- **缺点：** 无法低级访问 MIDI 2.0。
- **缺点：** 无虚拟端口时不支持多消费者。

### 选项 B：RtMidi.NET（RtMidi 的 .NET 封装）
- **优点：** 跨平台（Windows、macOS、Linux）。
- **缺点：** .NET 社区较小；需要原生 `rtmidi.dll`。
- **缺点：** 对于纯 Windows 项目无明显收益。

### 选项 C：Windows MIDI Services（Windows 11 24H2 起）
- **优点：** 原生多客户端支持，内置 MIDI 2.0。
- **缺点：** 仅支持 Windows 11 24H2+；用户基础仍较窄。
- **缺点：** API 尚不成熟（Beta 状态，截至 2025 年）。

## 决策

选择**选项 A（NAudio）**作为主要库，并通过抽象层（`IMidiInputDevice`）保留日后切换到选项 B 或 C 的可能性。

## 理由

- NAudio 经过充分验证，社区活跃，可直接通过 NuGet 获取。
- 约 1–2 ms 的延迟对于目标应用（键盘输入）已足够。
- 通过抽象层，当 Windows 11 最低系统要求可接受时，可以切换到 Windows MIDI Services。

## 影响

- 必须定义 `IMidiInputDevice` 接口。
- NAudio 作为 NuGet 依赖项引入（`NAudio`、`NAudio.WinMM`）。
- MIDI 2.0 功能暂不可用。
