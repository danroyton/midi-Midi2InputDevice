# ADR-004: 键盘和鼠标输入注入方法

| 字段 | 值 |
|---|---|
| 状态 | **已接受** |
| 日期 | 2025-05-30 |
| 决策者 | 开发团队 |

## 背景

后端必须向操作系统注入键盘和鼠标事件。Windows 提供多种 API，在延迟、兼容性和限制方面各有不同。

## 选项

### 选项 A：`SendInput`（user32.dll）
- 用于合成输入的官方 Win32 API。
- 支持键盘（`INPUT_KEYBOARD`）和鼠标（`INPUT_MOUSE`）。
- 支持批处理：在单次调用中发送多个事件。
- **优点：** 延迟低，文档完善，无需额外驱动程序。
- **缺点：** 被部分反作弊系统屏蔽。
- **缺点：** 在 Session 0（服务上下文）中无效（参见 ADR-002）。

### 选项 B：`keybd_event` / `mouse_event`（已过时）
- 较旧的 Win32 API，在内部重定向到 `SendInput`（自 Windows NT 起）。
- **缺点：** 已过时，相比选项 A 无任何优势。

### 选项 C：Windows Input Simulation（InputSimulator NuGet）
- `SendInput` 的 .NET 封装。
- **优点：** 比直接 P/Invoke 更易使用。
- **缺点：** 额外依赖项；薄薄的抽象层没有实质性增益。

### 选项 D：Virtual HID（ViGEm / HidHide）
- 虚拟 HID 游戏手柄/键盘驱动程序。
- **优点：** 非常适合游戏手柄模拟；被反作弊系统容忍。
- **缺点：** 需要安装内核驱动；对于纯键盘输入过于复杂。

## 决策

选择**选项 A（通过 P/Invoke 的 `SendInput`）**，封装在自定义 `InputInjector` 类后面，通过 `IInputInjector` 接口暴露。

使用自定义 P/Invoke 而非库（选项 C），因为范围有限，不需要额外依赖项。

## 理由

- `SendInput` 是推荐的、最现代的 Win32 合成输入 API。
- 批量调用减少了组合键和重复键时的 P/Invoke 开销。
- `IInputInjector` 接口允许日后扩展（例如，添加 ViGEm 支持游戏手柄）。

## 实现细节

```csharp
// P/Invoke 签名（摘要）
[DllImport("user32.dll", SetLastError = true)]
static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

// 按键保持（Y > 0）的执行顺序：
// 1. KeyDown
// 2. Thread.Sleep(Y)
// 3. KeyUp
// 重复（X > 1）的执行顺序：
// 步骤 1–3 重复 X 次，每次重复之间暂停 Z ms
```

## 影响

- `IInputInjector` 在领域层中定义为接口。
- `SendInput` 实现位于基础设施层。
- 反作弊兼容性**不是**本版本的目标；可以通过单独的任务评估选项 D。
- 对于按键保持（Y > 0），在注入线程上使用 `Thread.Sleep`——该线程独立于 HTTP 线程，不会阻塞 API 请求。
