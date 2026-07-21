# MyStarClient.Restart 硬编码 "StarAgent" 进程名，重命名后 node/restart 命令失效

## 📋 问题描述

`MyStarClient.Restart` 方法中调用 `upgrade.Run("StarAgent", ...)` 硬编码了进程名 `StarAgent`。当用户**重命名 StarAgent 可执行文件**（如改为 `MyAgent.exe`、`监控.exe`）后，`node/restart` 命令会失效——尝试启动名为 `StarAgent` 的进程，但实际进程名已改变，导致重启失败。

---

## 🔍 复现步骤

1. 将 `StarAgent.exe` 重命名为 `MyAgent.exe`（或在 Linux 上重命名为 `myagent`）
2. 启动 `MyAgent`，连接到 Server
3. 在节点在线页面点击"重启服务"（下发 `node/restart` 命令）
4. **失败**：尝试启动 `StarAgent` 进程，但文件不存在

**预期行为**：使用当前进程的可执行文件路径启动新进程，与进程名解耦。
**实际行为**：硬编码 `StarAgent` 名称，重命名后失效。

---

## 📍 问题定位

文件：[`StarAgent/MyStarClient.cs`](https://github.com/NewLifeX/Stardust/blob/master/StarAgent/MyStarClient.cs) `Restart` 方法

**官方代码**：

```csharp
protected override void Restart(Upgrade upgrade)
{
    var inService = "-s".EqualIgnoreCase(Environment.GetCommandLineArgs());
    var pid = Process.GetCurrentProcess().Id;

    // ...（中间逻辑省略）...

    if (inService || Service.Host is DefaultHost host && host.InService)
    {
        // 使用外部命令重启服务
        var rs = upgrade.Run("StarAgent", "-restart -delay", 3_000);
        // ...
    }
    else
    {
        // 重新拉起进程
        var rs = upgrade.Run("StarAgent", "-run -delay", 3_000);
        // ...
    }
}
```

### 根本原因

- `upgrade.Run("StarAgent", ...)` 第一个参数是进程名，硬编码为 `"StarAgent"`
- 用户重命名可执行文件后，`upgrade.Run` 找不到 `StarAgent` 文件
- Linux 环境下重命名更常见（如部署为 systemd 服务时改为 `stardust-agent`）

---

## 💡 建议修复方案

使用 `Process.GetCurrentProcess().MainModule.FileName` 获取当前进程的可执行文件路径，替代硬编码的 `"StarAgent"`：

```csharp
/// <summary>使用当前可执行文件路径启动新进程</summary>
private static Boolean RunCurrentProcess(String args)
{
    try
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName;
        if (exePath.IsNullOrEmpty()) return false;

        var si = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = args,
            UseShellExecute = false,
        };

        return Process.Start(si) != null;
    }
    catch { return false; }
}

protected override void Restart(Upgrade upgrade)
{
    var inService = "-s".EqualIgnoreCase(Environment.GetCommandLineArgs());

    if (inService || Service.Host is DefaultHost host && host.InService)
    {
        var rs = RunCurrentProcess("-restart -delay");
        // ...
    }
    else
    {
        var rs = RunCurrentProcess("-run -delay");
        // ...
    }
}
```

### 修复优势

- ✅ 与进程名解耦，重命名后仍可正常重启
- ✅ Windows/Linux 通用（`MainModule.FileName` 跨平台）
- ✅ 同时修复 `Upgrade` 方法中其他 `upgrade.Run("StarAgent", ...)` 调用

---

## 📊 影响评估

| 影响项 | 说明 |
|--------|------|
| 严重程度 | 中（功能失效，但有规避方案） |
| 触发频率 | 每次节点重启 |
| 影响范围 | 所有重命名 StarAgent 的用户 |
| 临时规避 | 不要重命名 StarAgent 可执行文件 |
| 平台 | Windows + Linux 均受影响 |

---

## 🙏 致谢

感谢 NewLife 团队的优秀开源工作！

---

**标签建议**：`bug`、`staragent`、`cross-platform`
