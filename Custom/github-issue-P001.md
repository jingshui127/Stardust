# WebSocket 连接断开时产生大量异常日志刷屏

## 📋 问题描述

StarAgent 运行时，**WebSocket 连接正常断开**（如服务端重启、网络抖动、客户端主动关闭）会产生大量异常日志，包括 `ObjectDisposedException`、`SocketException`、`WebSocketException` 等。这些异常属于预期内的行为，但被记录为 ERROR 级别，**导致日志文件被刷屏，掩盖真正的错误**。

---

## 🔍 复现步骤

1. 启动 StarAgent 连接到 Stardust.Server
2. 重启 Server，或断开网络片刻
3. 观察 StarAgent 日志：大量异常堆栈输出

**典型日志**（节选）：
```
WebSocket异常 ObjectDisposedException: 无法访问已释放的对象。
WebSocket异常 SocketException: 由于以前的关闭调用...
WebSocket异常 WebSocketException: ...
```

**预期行为**：WebSocket 连接断开是正常的网络行为，不应记录为 ERROR，或应合并/降级为 DEBUG。
**实际行为**：每次断开都输出完整异常堆栈，日志被刷屏。

---

## 📍 问题定位

问题源于 `NewLife.Remoting` 的 WebSocket 连接管理代码，在连接断开时未区分"正常关闭"和"异常断开"，统一抛出异常并记录 ERROR 日志。

受影响位置：
- StarAgent 的 `StarClient` 内部 WebSocket 客户端
- Stardust.Server 的 WebSocket 服务端推送

---

## 💡 建议修复方案

### 方案 A：在 NewLife.Remoting 层优化（推荐）

在 WebSocket 连接管理代码中，捕获以下异常时降级为 DEBUG 或不记录：

- `ObjectDisposedException` —— 连接已释放（正常关闭）
- `SocketException` 且 ErrorCode 为以下值时 —— 正常关闭：
  - `10054`（WSAECONNRESET）
  - `10053`（WSAECONNABORTED）
  - `995`（WSAECONNABORTED Linux）
- `WebSocketException` 且 WebSocketErrorCode 为 `ConnectionClosedPrematurely`

### 方案 B：提供日志过滤器扩展点

如果不想改 NewLife.Remoting，可在 `StarClient` 中提供 `LogFilter` 扩展点，允许上层过滤日志：

```csharp
public class StarClient : StarClientBase
{
    public Func<LogLevel, String, Boolean> LogFilter { get; set; }

    protected override void OnLog(LogLevel level, String format, params Object[] args)
    {
        if (LogFilter?.Invoke(level, String.Format(format, args)) == true) return;
        base.OnLog(level, format, args);
    }
}
```

### 方案 C：用户侧临时规避

用户可在 StarAgent 中包装日志过滤器（当前科控物联版本的实现）：

```csharp
internal class WebSocketLogFilter : Logger
{
    private readonly ILog _inner;
    public WebSocketLogFilter(ILog log) => _inner = log;

    protected override void OnWrite(LogLevel level, String format, params Object?[] args)
    {
        if (IsWebSocketNoise(format, args))
        {
#if DEBUG
            _inner.Write(level, "[Filtered] " + format, args);
#endif
            return;
        }
        _inner.Write(level, format, args);
    }

    private static Boolean IsWebSocketNoise(String format, Object?[] args)
    {
        // 检查 format 和 args 中的异常关键词
        // - "WebSocket异常"
        // - "无法访问已释放的对象"
        // - "由于以前的关闭调用"
        // - ObjectDisposedException / SocketException / WebSocketException
        // ...
    }
}

// Program.cs 中
XTrace.Log = new WebSocketLogFilter(XTrace.Log);
```

---

## 📊 影响评估

| 影响项 | 说明 |
|--------|------|
| 严重程度 | 低（日志噪音，不影响功能） |
| 触发频率 | 每次网络抖动/服务端重启 |
| 影响范围 | 所有 StarAgent 用户 |
| 临时规避 | 用户侧实现日志过滤器（见方案 C） |

---

## 💬 备注

当前我们用户侧已通过方案 C 临时解决，但希望官方能在 NewLife.Remoting 层根本性优化（方案 A），减少用户的定制成本。

---

## 🙏 致谢

感谢 NewLife 团队的优秀开源工作！

---

**标签建议**：`bug`、`logging`、`websocket`
