# StarClient.SetOomScoreAdj 对接管的非子进程写入失败，日志噪音且功能无效

## 📋 问题描述

StarAgent 接管**已存在的进程**（非 StarAgent 启动的子进程）时，会尝试设置该进程的 OOM 分值（`/proc/{pid}/oom_score_adj`）。但 Linux 权限规则下，**只有 root 或同用户进程才能写其他进程的 oom_score_adj**。StarAgent 以普通用户运行时，写入失败，输出错误日志。

这导致：
1. **日志噪音**：每次接管进程都输出错误日志
2. **功能无效**：设置 OOM 分值的目的是修正子进程继承父进程的 -1000，但对非子进程（已存在的进程）设置本来就没意义

---

## 🔍 复现步骤

1. 以普通用户启动 StarAgent（非 root）
2. 让 StarAgent 接管已存在的进程（如系统启动的 nginx、其他用户的 Java 应用等）
3. 观察日志：

```
设置进程[1742] OOM分值=0 失败：Access to the path '/proc/1742/oom_score_adj' is denied.
```

**预期行为**：接管已存在进程时不尝试设置 OOM 分值（或静默失败，不输出错误日志）。
**实际行为**：每次接管都输出错误日志，且 OOM 分值设置无效。

---

## 📍 问题定位

### 代码位置 1：`SetOomScoreAdj` 方法

文件：[`Stardust/StarClient.Linux.cs`](https://github.com/NewLifeX/Stardust/blob/master/Stardust/StarClient.Linux.cs) 第 1121-1135 行

```csharp
public static void SetOomScoreAdj(Int32 pid, Int32 score = 0)
{
    if (!Runtime.Linux) return;
    if (pid <= 0) return;

    try
    {
        var path = $"/proc/{pid}/oom_score_adj";
        File.WriteAllText(path, score.ToString());
    }
    catch (Exception ex)
    {
        XTrace.WriteLine("设置进程[{0}] OOM分值={1} 失败：{2}", pid, score, ex.Message);
    }
}
```

### 代码位置 2：接管进程时调用

文件：[`Stardust/Managers/ServiceController.cs`](https://github.com/NewLifeX/Stardust/blob/master/Stardust/Managers/ServiceController.cs) 第 696-699 行

```csharp
// OOM分值。接管已存在进程时修正其OOM分值，避免StarAgent重启后旧子进程仍保持 -1000
var oomScore = Info?.OomScoreAdjust ?? 0;
if (Runtime.Linux && oomScore != -1000)
    StarClient.SetOomScoreAdj(p.Id, oomScore);
```

### 根本原因

1. **Linux 权限规则**：`/proc/{pid}/oom_score_adj` 的写入权限仅限：
   - root 用户（CAP_SYS_RESOURCE）
   - 进程自身的同用户（即 StarAgent 启动的子进程，StarAgent 有权写）
2. **接管已存在进程**的场景下，被接管的进程**不是 StarAgent 的子进程**，可能是：
   - 系统启动的服务（如 nginx、mysql）
   - 其他用户启动的应用
   - 旧版 StarAgent 启动的孤儿进程（已换用户）
3. 这些场景下，StarAgent **无权写** `/proc/{pid}/oom_score_adj`

### 设计意图与实际的偏差

代码注释说"避免StarAgent重启后旧子进程仍保持 -1000"，但实际接管的不一定是"旧子进程"：

| 接管场景 | 是否 StarAgent 子进程？ | 能否写 oom_score_adj？ |
|---------|------------------------|----------------------|
| StarAgent 重启后接管自己的旧子进程 | ✅ 是（同用户） | ✅ 能 |
| 接管系统启动的服务（nginx 等） | ❌ 否 | ❌ 不能 |
| 接管其他用户启动的应用 | ❌ 否 | ❌ 不能 |

---

## 💡 建议修复方案

### 方案 A：接管前检查进程是否为 StarAgent 的子进程（推荐）

在 `ServiceController.TakeOver` 中，只对 StarAgent 启动的子进程设置 OOM 分值：

```csharp
// OOM分值。仅对 StarAgent 启动的子进程设置，避免对非子进程写入失败
var oomScore = Info?.OomScoreAdjust ?? 0;
if (Runtime.Linux && oomScore != -1000 && IsChildProcess(p))
    StarClient.SetOomScoreAdj(p.Id, oomScore);
```

其中 `IsChildProcess` 可通过检查进程的 PPID 是否为当前 StarAgent 进程来判断：

```csharp
private static Boolean IsChildProcess(Process p)
{
    try
    {
        // 读取 /proc/{pid}/stat 获取 PPID
        var stat = File.ReadAllText($"/proc/{p.Id}/stat");
        var parts = stat.Split(' ');
        if (parts.Length > 3)
        {
            var ppid = Int32.Parse(parts[3]);
            return ppid == Environment.ProcessId;
        }
    }
    catch { }
    return false;
}
```

### 方案 B：静默处理 UnauthorizedAccess 异常（最小改动）

在 `SetOomScoreAdj` 中，对 `UnauthorizedAccess` 异常不输出日志：

```csharp
public static void SetOomScoreAdj(Int32 pid, Int32 score = 0)
{
    if (!Runtime.Linux) return;
    if (pid <= 0) return;

    try
    {
        var path = $"/proc/{pid}/oom_score_adj";
        File.WriteAllText(path, score.ToString());
    }
    catch (UnauthorizedAccessException) { /* 权限不足，静默处理 */ }
    catch (Exception ex)
    {
        XTrace.WriteLine("设置进程[{0}] OOM分值={1} 失败：{2}", pid, score, ex.Message);
    }
}
```

### 方案 C：降级为 DEBUG 日志

将错误日志降级为 DEBUG，避免在生产环境刷屏：

```csharp
catch (Exception ex)
{
    // 权限不足是预期内的（接管非子进程时），降级为 DEBUG
    XTrace.Log.Debug("设置进程[{0}] OOM分值={1} 失败：{2}", pid, score, ex.Message);
}
```

---

## 📊 三种方案对比

| 方案 | 改动量 | 优点 | 缺点 |
|------|--------|------|------|
| A. 检查子进程 | ~15 行 | 治本，避免无效操作 | 需读 /proc/{pid}/stat |
| B. 静默 UnauthorizedAccess | 2 行 | 最小改动 | 仍会尝试写入并失败 |
| C. 降级 DEBUG | 1 行 | 隐藏日志噪音 | 仍会尝试写入并失败 |

**个人推荐**：方案 A（治本）+ 方案 B（兜底）组合使用。

---

## 📊 影响评估

| 影响项 | 说明 |
|--------|------|
| 严重程度 | 低（日志噪音，不影响功能） |
| 触发频率 | 每次接管非子进程时触发 |
| 影响范围 | 所有以非 root 用户运行 StarAgent 的 Linux 用户 |
| 临时规避 | 用 root 运行 StarAgent（不推荐，安全风险） |

---

## 🙏 致谢

感谢 NewLife 团队的优秀开源工作！

---

**标签建议**：`bug`、`linux`、`log-noise`
