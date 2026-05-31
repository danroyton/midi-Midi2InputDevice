# MidiController – Midi2InputDevice

将物理 MIDI 设备（键盘、鼓机、编码器控制器等）连接到 Windows，并将 MIDI 事件转换为任意键盘输入——无需驱动程序或第三方中间件。

> **当前版本：v0.3**
> Windows x64 单 EXE——无需安装程序，无需 .NET 运行时。

---

## 目录

- [功能特性](#功能特性)
- [快速入门](#快速入门)
- [架构](#架构)
- [配置](#配置)
- [开发](#开发)
- [创建发布版本](#创建发布版本)
- [路线图](#路线图)
- [限制](#限制)

---

## 功能特性

### MIDI 处理
- 从所有连接的物理 MIDI 设备接收输入（NAudio `MidiIn`）
- 设备更换时自动重连
- 支持的事件类型：`NoteOn`、`NoteOff`、`ControlChange`、`ProgramChange`、`PitchBend`
- 通过 `System.Threading.Channels` 实现无锁处理

### 映射引擎
- 触发器可按：设备、事件类型、通道、Data1 过滤器进行配置
- **状态变量 A–Z**（−127…+127），用于复杂控制逻辑
- **差值追踪**：`V`/`W` 保存与上次事件的差值
- **条件块**：块之间为 AND，块内为 OR
- **全局预/后阶段**：在动作前后设置变量并发送 MIDI 命令
- **ELSE 分支**：条件块失败时的替代动作
- **激活门控**（变量 `A`）：`0`=活动，`1`=暂停，`2`=锁定

### MIDI 输出
- 向物理 MIDI 输出设备发送 MIDI 命令（作为触发器中的预/后步骤）
- 支持的类型：`NoteOn`、`NoteOff`、`ControlChange`、`ProgramChange`、`PitchBend`

### 输入注入
- Win32 `SendInput`（P/Invoke）：单键和组合键
- `X` = 重复次数，`Y` = 保持时长（ms），`Z` = 按键后暂停（ms）
- 结构化按键组合编辑器：最多 2 个修饰键 + 1 个主键

### 配置与模板
- 配置文件以 JSON 格式存储在 `%APPDATA%\MidiController\`
- 可复用的条件和动作模板
- 触发器支持自定义显示名称
- 无需重启即可实时激活

### 前端（WPF）
- **状态视图**：门控控制，实时变量表（A–Z）
- **MIDI 日志视图**：所有 MIDI 事件的实时流，设备过滤器
- **设备视图**：连接/断开物理 MIDI 设备
- **映射视图**：完整触发器编辑器（预/后、条件、动作、ELSE）
- **模板视图**：管理模板
- **键盘测试视图**：实时测试键盘输入
- **系统托盘图标**：MIDI 活动时闪烁，根据门控状态颜色编码
- 单 EXE：后端（Kestrel）以进程内方式在前端进程中运行

---

## 快速入门

### 方式 A – 预构建 EXE（推荐）

1. 下载[最新版本](https://github.com/danroyton/midi-Midi2InputDevice/releases/latest)
2. 解压 ZIP
3. 启动 `MidiController.Frontend.exe`
4. 在**状态**选项卡中点击**激活**
5. 在**设备**选项卡中打开 MIDI 设备
6. 在**映射**选项卡中创建配置文件并配置触发器

无需安装程序，无需 .NET 运行时。

### 方式 B – 从源代码构建

```bash
git clone https://github.com/danroyton/midi-Midi2InputDevice.git
cd Midi2InputDevice
dotnet run --project MidiController.Frontend
```

---

## 架构

后端（Kestrel）和前端（WPF）**在同一进程中运行**：

```
MidiController.Frontend.exe
├─ WPF UI  ──────────────────────────────────── 用户界面
└─ BackendHostService（Kestrel :5000）
   ├─ REST API  /api/...
   ├─ SignalR   /hubs/status
   └─ SignalR   /hubs/midilog
		│
		├─ MidiInputService（NAudio，物理设备）
		├─ MidiOutputService（NAudio，MIDI 输出）
		├─ MappingEngine（触发器评估）
		└─ JsonConfigStore（%APPDATA%\MidiController\）
```

项目结构：

| 项目 | 说明 |
|---|---|
| `MidiController.Domain` | 领域模型、接口、枚举 |
| `MidiController.Engine` | 映射引擎、触发器执行器、门控、差值追踪 |
| `MidiController.Infrastructure` | MIDI 输入/输出（NAudio）、SendInput（Win32）、JSON 配置 |
| `MidiController.Host` | ASP.NET Core、REST API、SignalR Hub、`BackendStartup` |
| `MidiController.Frontend` | WPF 应用，以进程内方式集成后端主机 |

---

## 配置

### appsettings.json（前端）
```json
{
  "Backend": {
	"BaseUrl": "http://localhost:5000"
  }
}
```

### appsettings.backend.json（后端，EXE 旁边）
```json
{
  "MidiController": {
	"DataPath": "%APPDATA%\\MidiController",
	"EventChannelCapacity": 512
  },
  "Urls": "http://localhost:5000"
}
```

配置文件以 JSON 格式存储在 `%APPDATA%\MidiController\profiles\`。

---

## 开发

### 前置条件
- **Windows 10/11**（64 位）
- **Visual Studio 2022/2026**，包含工作负载：
  - `.NET 桌面开发`
  - `ASP.NET 和 Web 开发`
- **.NET 10 SDK**

### 构建与运行

```bash
# 调试运行（前端以进程内方式启动后端）
dotnet run --project MidiController.Frontend

# 测试
dotnet test

# 发布构建
dotnet build -c Release

# 单文件发布
dotnet publish MidiController.Frontend\MidiController.Frontend.csproj \
  /p:PublishProfile=SingleFile -c Release
```

输出：`MidiController.Frontend\bin\Release\publish\MidiController.Frontend.exe`

---

## 创建发布版本

完整的 GitHub 发布创建指南请参阅 [`docs/RELEASE.md`](MidiController/docs/RELEASE.md)。

快速概览：
1. 在 `CHANGELOG` 中记录版本
2. 设置 Git 标签：`git tag v0.3.0`
3. GitHub Actions 工作流 `release.yml` 自动构建并发布

---

## 路线图

| 版本 | 目标 |
|---|---|
| ✅ v0.1 | 基础框架：后端、引擎、REST API、WPF 前端 |
| ✅ v0.2 | 托盘图标、键盘测试视图、结构化按键组合编辑器 |
| ✅ v0.3 | 完整触发器编辑器（预/后、条件、ELSE）、MIDI 输出、单 EXE |
| 🔜 v0.4 | 虚拟 MIDI 端口（loopMIDI / Windows MIDI Services） |
| 🔜 v0.5 | Linux 后端 + Avalonia 前端（跨平台） |
| 🔜 v1.0 | Windows 服务模式、安装程序（MSI）、完整文档 |

---

## 限制

| 限制 | 详情 |
|---|---|
| **仅支持 Windows** | `SendInput` 和 NAudio `MidiIn` 是 Windows 特定的 |
| **无虚拟 MIDI 端口** | loopMIDI 集成待实现（v0.4） |
| **端口 5000 固定** | 后端始终运行在 `http://localhost:5000`；可在 `appsettings.backend.json` 中配置 |
| **无身份验证** | REST API 无认证；仅供本地使用 |
| **服务模式下的 SendInput** | 在无会话转发的 Windows 服务（Session 0）中不可用 |
