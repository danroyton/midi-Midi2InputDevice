# ADR-005: 前端技术选型

| 字段 | 值 |
|---|---|
| 状态 | **已接受** |
| 日期 | 2025-05-30 |
| 更新 | 2025-05-30（补充 Avalonia UI 评估） |
| 决策者 | 开发团队 |

## 背景

前端是一个 Windows 配置应用程序。它需要实时显示 MIDI 原始数据，渲染复杂的触发器配置表单，并通过 REST 和 WebSocket 与后端通信。

## 选项

### 选项 A：WPF（.NET 10）
- 成熟的 Windows GUI 框架。
- MVVM 模式配合 `CommunityToolkit.Mvvm`。
- **优点：** 原生 Windows 外观；无需浏览器运行时。
- **优点：** 如有需要可直接访问 Windows API。
- **优点：** 完全支持 .NET 10。
- **缺点：** 仅限 Windows，不跨平台。

### 选项 B：WinUI 3 / Windows App SDK
- 比 WPF 更现代的 UI 平台。
- **优点：** 现代 Fluent Design。
- **缺点：** 入门门槛较高；社区资源少于 WPF。
- **缺点：** 部署更复杂（推荐 MSIX 包）。

### 选项 C：.NET MAUI
- 跨平台（Windows、macOS、iOS、Android）。
- **优点：** 面向未来，一套代码库适配所有平台。
- **缺点：** Windows 目标内部使用 WinUI 3；稳定性尚不如 WPF。
- **缺点：** 对于纯 Windows 配置应用过于复杂。

### 选项 E：Avalonia UI（.NET 10）
- 基于 .NET 的跨平台 GUI 框架；通过 Skia（SkiaSharp）完全自绘渲染。
- 完整支持 `CommunityToolkit.Mvvm` 和 ReactiveUI。
- 自版本 11 起 API 稳定，持续活跃开发。
- **优点：** 跨平台（Windows、macOS、Linux），无需 WinRT 或 MSIX。
- **优点：** 现代、高度可定制的 UI；深色模式和自定义主题易于实现。
- **优点：** 无 Electron/浏览器开销；二进制体积精简。
- **优点：** 完全支持 .NET 10；基于 XAML——WPF 知识可迁移。
- **缺点：** 无原生 Windows 外观（Skia 渲染）；在 Windows 上与 WPF/WinUI 视觉效果不同。
- **缺点：** 社区较小，第三方控件少于 WPF。
- **缺点：** Visual Studio 中的设计工具支持较弱（无官方 XAML 设计器；JetBrains Rider 支持更好）。
- **缺点：** 原生 Windows API 访问（如托盘集成）需要平台特定代码。

**对本项目的评估：**
由于目标平台主要是纯 Windows，且后端 Win32 特定（`SendInput`、MIDI WinMM），前端跨平台没有直接价值。对于纯配置工具，Avalonia 是 WPF 的可行替代方案——如果优先考虑现代样式或日后的 macOS/Linux 移植的话。缺少 Visual Studio 设计器会显著增加 UI 开发的工作量。

### 选项 D：Electron / Web 前端（React/Vue）
- 在浏览器窗口中运行 UI。
- **优点：** 丰富的 UI 组件生态系统。
- **缺点：** 体积较大（Node.js、Chromium）。
- **缺点：** 无原生 Windows 外观。

## 决策

**选项 A（WPF，.NET 10）**——并已对选项 E（Avalonia UI）进行了明确评估。

## 理由

- 目标平台仅为 Windows；跨平台无附加价值。
- WPF 成熟、文档完善，且在 .NET 10 生态中完全受支持。
- `CommunityToolkit.Mvvm` 将样板代码降至最低。
- 团队具备现有的 .NET 知识——无需 JavaScript/TypeScript 背景。
- **相比 Avalonia：** WPF 提供完整的 Visual Studio XAML 设计器、更大的社区和原生 Windows 渲染。由于后端仍将是 Win32 特定的（SendInput、MIDI WinMM），跨平台不是论据。若项目需要扩展至 macOS/Linux，由于 XAML 相似性，切换至 Avalonia 作为替代方案是可行的。

## 影响

- 在解决方案中创建新的 WPF 项目 `KarentaMidi2DeviceInput.UI`。
- NuGet 依赖项：`CommunityToolkit.Mvvm`、`Microsoft.AspNetCore.SignalR.Client`、Polly。
- 前端不跨平台；由于 MVVM ViewModel 层与框架无关（只需替换 View 层），日后切换至 Avalonia UI 是可行的。
