# MidiController – 整体架构

## 概述

**MidiController** 接收来自物理 MIDI 设备的事件，并将其转换为系统输入（按键操作）。后端与前端**在同一进程中运行**（`MidiController.Frontend.exe`）。WPF 应用程序启动时，后端（Kestrel 主机）以进程内方式启动，并通过本地 REST API 和 SignalR 与 UI 进行通信。

```
┌─────────────────────────────────────────────────────────────────┐
│                     用户（Windows）                              │
└────────────────────────────┬────────────────────────────────────┘
							 │ 鼠标 / 键盘 / 托盘
┌────────────────────────────▼────────────────────────────────────┐
│            MidiController.Frontend.exe                          │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  WPF UI（.NET 10-windows）                              │    │
│  │  状态 · MIDI 日志 · 设备 · 映射 · 模板                  │    │
│  │  键盘测试 · 托盘图标                                    │    │
│  └─────────────────┬───────────────────────────────────────┘    │
│                    │ REST  http://localhost:5000                 │
│                    │ SignalR /hubs/status /hubs/midilog          │
│  ┌─────────────────▼───────────────────────────────────────┐    │
│  │  BackendHostService（Kestrel 进程内）                   │    │
│  │                                                         │    │
│  │  ┌─────────────┐  ┌──────────────────┐  ┌───────────┐  │    │
│  │  │ MidiInput   │  │  MappingEngine   │  │ SendInput │  │    │
│  │  │ Service     │─▶│  （触发器/门控/  │─▶│（Win32）  │  │    │
│  │  │（NAudio）   │  │   变量 A-Z）     │  └───────────┘  │    │
│  │  └─────────────┘  └──────────────────┘                 │    │
│  │  ┌─────────────┐  ┌──────────────────┐                 │    │
│  │  │ MidiOutput  │  │  JsonConfigStore │                 │    │
│  │  │ Service     │  │  (%APPDATA%\     │                 │    │
│  │  │（NAudio）   │  │   MidiController)│                 │    │
│  │  └─────────────┘  └──────────────────┘                 │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

## 项目层次

| 项目 | 技术 | 职责 |
|---|---|---|
| `MidiController.Domain` | .NET 10 | 领域模型（`MidiEvent`、`Trigger`、`Profile`、变量、枚举、接口） |
| `MidiController.Engine` | .NET 10 | 映射引擎、触发器执行器、门控逻辑、差值追踪、变量存储 |
| `MidiController.Infrastructure` | .NET 10 / Win32 | MIDI 输入/输出（NAudio）、`SendInput`（P/Invoke）、JSON 配置存储 |
| `MidiController.Host` | ASP.NET Core .NET 10 | Kestrel、REST API、SignalR Hub、DI 配置、`BackendStartup` 入口 |
| `MidiController.Frontend` | WPF .NET 10-windows | 主应用程序；以进程内方式启动后端，提供 UI |

## 启动顺序

```
App.xaml.cs → OnStartup()
  1. BackendHostService.StartAsync()        ← 以进程内方式启动 Kestrel
  2. Build ServiceProvider                  ← 注册 ViewModel、服务
  3. TrayService.Initialize()               ← 创建 NotifyIcon
  4. 创建并显示主窗口
  5. StatusViewModel 连接 SignalR
```

## 进程内后端托管

`MidiController.Host` 中的 `BackendStartup` 入口封装了完整的后端基础设施：

```csharp
// MidiController.Host/BackendStartup.cs
public static Task StartAsync(
	string configPath,
	string url = "http://localhost:5000",
	CancellationToken ct = default)
```

优点：
- **单进程，单 EXE** – 无需单独启动后端
- **无 IPC 复杂性** – 可直接使用内存通道
- **分发简便** – 单文件发布即可

## 数据流

```
物理 MIDI 设备
  │  USB/DIN
  ▼
NAudio MidiIn.MessageReceived
  │
  ▼  Channel<MidiEvent>（容量=512，无锁）
MappingEngine
  │  评估触发器 → 检查门控 → 读/写变量
  │  全局预赋值 + 预 MIDI 发送
  │  评估条件块
  │  执行动作块（SendInput）
  │  全局后赋值 + 后 MIDI 发送
  ▼
Win32 SendInput
  │
  ▼
活动应用程序中的 Windows 键盘输入

同时：
MidiEvent → SignalR Hub /hubs/midilog → WPF MIDI 日志视图
变量变化 → SignalR Hub /hubs/status → WPF 状态视图
```

## 配置文件

| 文件 | 位置 | 用途 |
|---|---|---|
| `appsettings.json` | EXE 旁边 | 前端配置（后端 URL） |
| `appsettings.backend.json` | EXE 旁边 | 后端配置（DataPath、Kestrel URL） |
| `profiles\{name}.json` | `%APPDATA%\MidiController\` | 触发器/映射配置文件 |
| `templates\{name}.json` | `%APPDATA%\MidiController\` | 可复用条件/动作模板 |

## 架构决策

ADR 文件中的正式说明：

| ADR | 决策 |
|---|---|
| [ADR-001](adr/ADR-001-midi-library.zh.md) | NAudio 作为 MIDI 输入库 |
| [ADR-002](adr/ADR-002-midi-mapper-service.zh.md) | 映射器作为独立后台服务 |
| [ADR-003](adr/ADR-003-virtual-midi-devices.zh.md) | 虚拟 MIDI 端口（待实现） |
| [ADR-004](adr/ADR-004-input-injection.zh.md) | Win32 `SendInput` 替代虚拟输入设备 |
| [ADR-005](adr/ADR-005-frontend-technology.zh.md) | WPF 作为前端技术 |
| [ADR-006](adr/ADR-006-ipc-api.zh.md) | REST + SignalR 作为 IPC 通道 |
