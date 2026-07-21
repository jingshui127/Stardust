# Linux 无 WiFi 设备时 MachineInfoProvider 频繁输出 `command failed: No such device (-19)` 日志噪音

## 📋 问题描述

在**没有无线网卡**的 Linux 设备上运行 StarAgent 时，日志中会持续刷屏如下错误：

```
command failed: No such device (-19)
command failed: No such device (-19)
command failed: No such device (-19)
...（每次心跳上报一次）
```

该错误来自 `iw` 命令（Linux 无线网络配置工具）的 stderr 输出，但**不影响实际功能**，仅是日志噪音。

---

## 🔍 复现环境

| 项 | 版本 |
|----|------|
| 操作系统 | Linux（无无线网卡的设备，如服务器/工控机/容器） |
| NewLife.Stardust | 3.9.2026.716-beta1411 |
| StarAgent | 3.7.2026.0720 |
| .NET | 10.0 |

---

## 📍 问题定位

文件：[`Stardust/Managers/MachineInfoProvider.cs`](https://github.com/NewLifeX/Stardust/blob/master/Stardust/Managers/MachineInfoProvider.cs) 第 210-255 行

`Refresh(MachineInfo info)` 方法在 Linux 环境下采集 WiFi 信号强度时的逻辑：

```csharp
else if (Runtime.Linux)
{
    var signal = 0;
    var file = "/proc/net/wireless";
    if (File.Exists(file))
    {
        var line = File.ReadAllLines(file)?.LastOrDefault();
        if (!line.IsNullOrEmpty())
        {
            var ss = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (ss.Length > 3)
            {
                info["Signal"] = signal = (ss[3]?.TrimEnd('.')).ToInt();
            }
        }
    }

    if (signal == 0)
    {
        // ↓↓↓ 问题所在：未检查设备是否存在就直接调用 iw
        var rs = Execute("iw", "dev wlan0 link", 1_000);
        if (!rs.IsNullOrEmpty())
        {
            // ...解析 SSID 和 signal...
        }
    }
}
```

### 根本原因

1. Linux 设备**没有 `wlan0` 网卡**时，`/proc/net/wireless` 不存在或为空，`signal == 0`
2. 进入 fallback 分支调用 `iw dev wlan0 link`
3. `iw` 找不到 `wlan0` 设备，向 **stderr** 输出 `command failed: No such device (-19)`
4. 虽然代码只重定向了 stdout（`RedirectStandardError = false`），但 `iw` 的 stderr 默认继承父进程，直接写入 StarAgent 的控制台/日志
5. **每次心跳上报机器信息都会触发一次**，造成日志持续刷屏

### 触发频率

每次 `MachineInfoProvider.Refresh()` 调用都触发一次，与心跳周期一致（默认约 5-60 秒一次）。

---

## 💡 建议修复方案

### 方案 A：检查 wlan0 设备是否存在（推荐）

在调用 `iw` 前，先检查 `/sys/class/net/wlan0` 是否存在：

```csharp
if (signal == 0)
{
    // 先检查 wlan0 设备是否存在，避免无 WiFi 设备时 iw 命令刷屏
    if (!Directory.Exists("/sys/class/net/wlan0")) return;

    var rs = Execute("iw", "dev wlan0 link", 1_000);
    if (!rs.IsNullOrEmpty())
    {
        // ...原有解析逻辑...
    }
}
```

**优点**：
- ✅ 最小改动（2 行代码）
- ✅ 不影响有 WiFi 设备的机器（`/sys/class/net/wlan0` 在有无线网卡时一定存在）
- ✅ 同时避免 `iw` 进程创建开销（无需启动进程就知道没有设备）

### 方案 B：动态查找无线网卡名

如果考虑网卡名不一定是 `wlan0`（如 `wlp2s0`、`wlan1` 等），可动态查找：

```csharp
if (signal == 0)
{
    // 动态查找无线网卡，避免硬编码 wlan0
    var wirelessIfs = NetworkInterface.GetAllNetworkInterfaces()
        .Where(e => e.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
                    && e.OperationalStatus == OperationalStatus.Up)
        .Select(e => e.Name)
        .ToList();

    if (wirelessIfs.Count == 0) return;

    foreach (var ifName in wirelessIfs)
    {
        var rs = Execute("iw", $"dev {ifName} link", 1_000);
        if (!rs.IsNullOrEmpty())
        {
            // ...原有解析逻辑...
            if (signal != 0) break;
        }
    }
}
```

**优点**：
- ✅ 兼容各种网卡命名（wlan0/wlp2s0/wlan1 等）
- ✅ 多无线网卡场景下逐个尝试

### 方案 C：重定向 stderr 避免污染日志

如果不想改判断逻辑，至少应该把 stderr 重定向，避免噪音写入日志：

```csharp
var psi = new ProcessStartInfo(cmd, arguments ?? String.Empty)
{
    UseShellExecute = false,
    CreateNoWindow = true,
    WindowStyle = ProcessWindowStyle.Hidden,
    RedirectStandardOutput = true,
    RedirectStandardError = true,  // ← 改为 true，避免 stderr 继承父进程
};
```

`Execute` 方法第 290 行的注释 `//RedirectStandardError = true,` 可以启用。

---

## 📊 三种方案对比

| 方案 | 改动量 | 优点 | 缺点 |
|------|--------|------|------|
| A. 检查 `/sys/class/net/wlan0` | 2 行 | 最小改动、避免进程创建 | 仍硬编码 `wlan0` |
| B. 动态查找无线网卡 | ~10 行 | 兼容各种网卡命名 | 改动稍大 |
| C. 重定向 stderr | 1 行 | 仅隐藏日志，不解决根本问题 | 仍会启动 `iw` 进程 |

**个人推荐**：方案 A + 方案 C 组合使用（既避免不必要的进程创建，又彻底屏蔽 stderr 噪音）。

---

## 📎 参考资料

- Linux errno 19 = ENODEV（No such device）
- `iw` 命令文档：https://wireless.wiki.kernel.org/en/users/Documentation/iw
- 相关代码位置：`Stardust/Managers/MachineInfoProvider.cs:234`

---

## 🙏 致谢

感谢 NewLife 团队的优秀开源工作！这是一个小的体验优化建议，希望对项目有所帮助。

---

**标签建议**：`bug`、`linux`、`log-noise`
