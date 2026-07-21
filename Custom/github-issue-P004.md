# NewLife.Agent.Setting.WebPort 默认值 5580 与 StarAgent 的 LocalPort 冲突，导致 WebPanel 默认配置无法启动

## 📋 问题描述

`NewLife.Agent.Setting.WebPort` 的默认值是 **5580**，与 `StarAgentSetting.LocalPort` 的默认值 **5580** 冲突。两者底层都监听 TCP 端口，导致**默认配置下 StarAgent 启动时 WebPanel 无法绑定端口，管理面板功能完全不可用**。

---

## 🔍 复现步骤

1. 全新部署 StarAgent（不修改任何配置）
2. 启动 StarAgent
3. 访问 `http://127.0.0.1:5580/`（WebPanel 默认地址）

**预期行为**：WebPanel 正常显示管理界面。
**实际行为**：WebPanel 绑定 5580 端口失败（被 ApiServer 占用），管理面板无法访问。

### 启动顺序

`ServiceBase.StartWork()` 的执行顺序：
1. 先创建 `ApiServer`，监听 `tcp://0.0.0.0:5580` + `udp://0.0.0.0:5580`（设置 `ReuseAddress=true`）
2. 再创建 `WebPanel`，尝试监听 `http://0.0.0.0:5580/`（HTTP 底层是 TCP）
3. **WebPanel 绑定 TCP 5580 失败**（ApiServer 已占用 TCP 5580，`ReuseAddress` 仅对同协议生效，HTTP 与 ApiServer 的 TCP 不可共享）

---

## 📍 问题定位

### 冲突的默认值

| 配置项 | 默认值 | 协议 | 所在类 |
|--------|--------|------|--------|
| `StarAgentSetting.LocalPort` | **5580** | TCP + UDP（ApiServer） | `StarAgent.Setting.cs` |
| `NewLife.Agent.Setting.WebPort` | **5580** | HTTP（WebPanel，底层 TCP） | `NewLife.Agent.Setting.cs`（NuGet 包内） |

### 关键代码

`StarAgent/Program.cs` 中 `StartLocalServer`：

```csharp
public void StartLocalServer(Int32 port)
{
    // ApiServer 同时监听 TCP + UDP，ReuseAddress=true 允许端口复用
    var svr = new ApiServer(port)
    {
        ReuseAddress = true,
        // ...
    };
    // 占用 TCP 5580 + UDP 5580
}
```

`NewLife.Agent.ServiceBase` 中 WebPanel 创建（NuGet 包内）：

```csharp
// WebPanel 默认监听 http://0.0.0.0:5580/，底层 TCP 5580
// 但 TCP 5580 已被 ApiServer 占用，绑定失败
```

### 根本原因

- `NewLife.Agent.Setting.WebPort` 的默认值 `5580` 没有考虑到 StarAgent 的 `LocalPort` 也是 `5580`
- `ReuseAddress=true` 仅允许同协议（UDP 与 UDP、TCP 与 TCP 之间在某些 OS 下可复用），但 HTTP 和 ApiServer 的 TCP 通信**不可共享端口**
- 用户必须手动修改 `Agent.config` 中的 `<WebPort>` 才能使用 WebPanel

---

## 💡 建议修复方案

### 方案 A：修改 WebPort 默认值（推荐）

将 `NewLife.Agent.Setting.WebPort` 的默认值从 `5580` 改为 `5581`（或其他不冲突的端口）：

```csharp
// NewLife.Agent/Setting.cs
[IntDescription("Web管理面板端口。默认5581")]
public Int32 WebPort { get; set; } = 5581;
```

**优点**：
- ✅ 开箱即用，默认配置即可正常工作
- ✅ 1 行代码修复，向后兼容（已修改配置的用户不受影响）
- ✅ 5581 与 5580 数字相邻，便于记忆

### 方案 B：WebPanel 启动时检测端口冲突

在 `ServiceBase` 创建 WebPanel 前，检测 WebPort 是否已被占用，若被占用则自动 +1 或输出警告：

```csharp
// ServiceBase.StartWork() 中创建 WebPanel 前
if (IsPortInUse(Setting.WebPort))
{
    XTrace.WriteLine($"WebPanel 端口 {Setting.WebPort} 已被占用，尝试使用 {Setting.WebPort + 1}");
    Setting.WebPort++;
    Setting.Save();
}
```

**优点**：
- ✅ 自动规避冲突，无需修改默认值
- ⚠️ 但可能与其他服务冲突（如 5581 也被占用）

### 方案 C：StarAgent 启动时强制设置 WebPort

在 `StarAgent.Program.Main` 中检测 `IsNew` 时设置 `WebPort = 5581`：

```csharp
var agentConfig = NewLife.Agent.Setting.Current;
if (agentConfig.IsNew)
{
    agentConfig.WebPort = 5581;  // 避免 LocalPort 5580 冲突
    agentConfig.Save();
}
```

**优点**：
- ✅ 在 StarAgent 层修复，不依赖 NewLife.Agent 发版
- ⚠️ 仅对 StarAgent 生效，其他使用 NewLife.Agent 的项目仍有问题

---

## 📊 影响评估

| 影响项 | 说明 |
|--------|------|
| 严重程度 | 高（开箱即用失败） |
| 触发频率 | 每次首次部署 StarAgent |
| 影响范围 | 所有使用默认配置的用户 |
| 临时规避 | 手动修改 `Agent.config` 中 `<WebPort>5581</WebPort>` |

---

## 💬 备注

这是一个**开箱即用**的问题：新用户首次部署 StarAgent 时，默认配置无法使用 WebPanel，必须查阅文档手动修改端口。这会影响首次使用体验。

当前我们用户侧已通过方案 C 临时解决（在 `Program.cs` 中 `IsNew` 时设置 `WebPort = 5581`），但希望官方能从根本修复（方案 A），让所有 NewLife.Agent 用户受益。

---

## 🙏 致谢

感谢 NewLife 团队的优秀开源工作！

---

**标签建议**：`bug`、`configuration`、`webpanel`
