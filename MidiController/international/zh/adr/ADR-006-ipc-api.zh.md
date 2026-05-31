# ADR-006: 前端与后端的通信协议

| 字段 | 值 |
|---|---|
| 状态 | **已接受** |
| 日期 | 2025-05-30 |
| 决策者 | 开发团队 |

## 背景

前端和后端需要通信。后端作为本地进程运行在同一台机器上。存在两种通信模式：

1. **请求/响应** — 读写配置、列举设备、激活配置文件。
2. **推送/流** — 实时向前端发送 MIDI 原始数据、状态变量变更。

## 选项

### 选项 A：REST（HTTP）+ SignalR WebSocket
- REST 用于请求/响应；SignalR 用于流式传输。
- **优点：** 职责清晰分离；REST 易于测试（Swagger/OpenAPI）。
- **优点：** SignalR 提供自动重连和传输回退。
- **优点：** 两者都在 ASP.NET Core 中开箱即用。
- **缺点：** 两种协议；前端需要两种客户端类型。

### 选项 B：gRPC + gRPC 服务端流式传输
- 二进制协议，强类型（Protobuf）。
- **优点：** 序列化开销低于 JSON。
- **缺点：** 对于本地通信，开销差异可以忽略不计。
- **缺点：** 工具链更复杂；WPF 客户端集成不够普遍。

### 选项 C：命名管道（IPC）
- 直接使用 Windows IPC。
- **优点：** 本地通信延迟最低。
- **缺点：** 非标准 HTTP；测试和调试更困难。
- **缺点：** 不支持浏览器访问；前端替换更困难。

### 选项 D：仅 WebSocket（无 REST）
- 所有消息通过一个 WebSocket 通道传输。
- **缺点：** 请求/响应模式必须手动实现（关联 ID）。
- **缺点：** 无法使用标准工具（如 Swagger）。

## 决策

**选项 A：ASP.NET Core Minimal API（REST/JSON）+ SignalR。**

- **REST** 用于所有 CRUD 操作和配置管理。
- **SignalR**（WebSocket）用于实时流：MIDI 原始日志和状态变量。
- 启用 **OpenAPI/Swagger**，便于开发和测试。
- 端口：`localhost:5173`（可在 `appsettings.json` 中配置）。

## 理由

- 两种技术都原生集成在 ASP.NET Core 中——不需要额外框架。
- REST + OpenAPI 支持后端的独立开发和手动测试。
- SignalR 的自动重连和传输回退减少了客户端工作量。
- 配置操作的 REST 端点延迟无关紧要。
- 对于实时流（MIDI 日志），SignalR 的 WebSocket 延迟已足够，因为日志仅用于展示，不在关键注入路径上。

## 影响

- 后端：`app.MapControllers()` / Minimal API + `app.MapHub<MidiLogHub>("/hubs/midilog")`。
- 前端：`HttpClient`（REST）+ `HubConnection`（SignalR）。
- 为 `localhost` 配置 CORS。
- API 版本化：`/api/v1/` 前缀；未来的破坏性变更递增版本号。
- Swagger UI 在开发模式下可通过 `/swagger` 访问。
