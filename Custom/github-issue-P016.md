# MyService.OnServiceChanged 用 ServiceManager 内存中的旧值覆盖配置文件，导致 WebPanel 修改的服务参数丢失

## 📋 问题描述

用户通过 WebPanel 修改服务的 `WorkingDirectory`、`Arguments` 等参数后，触发 Start/Stop 操作会调用 `RaiseServiceChanged()`，进而触发 `OnServiceChanged` 事件。当前实现把 `_Manager.Services`（ServiceManager 内存中的旧值）整个替换到 `set.Services` 并 `Save()`，**覆盖了用户刚刚通过 WebPanel 修改并持久化的新参数**。

---

## 🔍 复现步骤

1. 启动 StarAgent，通过 WebPanel 修改某服务的 `WorkingDirectory`（WebPanel 内部会调用 `set.Save()` 写入新值到 `StarAgent.config`）
2. 通过 WebPanel 启动该服务（触发 `RaiseServiceChanged` → `OnServiceChanged`）
3. 检查 `StarAgent.config` —— `WorkingDirectory` 被回退为旧值

**预期行为**：Start/Stop 只更新 Enable 状态，保留用户通过 WebPanel 修改的其他参数。
**实际行为**：整个 Services 数组被内存中的旧值覆盖。

---

## 📍 问题定位

文件：[`StarAgent/Program.cs`](https://github.com/NewLifeX/Stardust/blob/master/StarAgent/Program.cs) `OnServiceChanged` 方法

**官方代码**：

```csharp
private void OnServiceChanged(Object? sender, EventArgs eventArgs)
{
    // 服务改变时，保存到配置文件
    var set = AgentSetting;
    set.Services = _Manager.Services.Select(e => e.Clone()).ToArray();
    set.Save();
}
```

### 根本原因

- `_Manager.Services` 是 `ServiceManager` 内存中的服务列表，**不包含用户通过 WebPanel 修改的字段**（如 WorkingDirectory、Arguments）
- WebPanel 修改时直接写入 `AgentSetting.Services` 并 `Save()`，但**没有同步更新 `_Manager.Services` 内存中的对象**
- Start/Stop 操作触发 `RaiseServiceChanged` 时，`OnServiceChanged` 用内存中的旧值覆盖了配置文件中的新值

### 触发场景

| 操作 | 是否触发 | 后果 |
|------|---------|------|
| Start 服务 | ✅ 触发 `OnServiceChanged` | 旧值覆盖新值 |
| Stop 服务 | ✅ 触发 `OnServiceChanged` | 旧值覆盖新值 |
| Restart 服务 | ✅ 触发 `OnServiceChanged` | 旧值覆盖新值 |

---

## 💡 建议修复方案

`OnServiceChanged` 应该只更新需要持久化的状态字段（如 `Enable`），不替换整个 Services 数组：

```csharp
private void OnServiceChanged(Object? sender, EventArgs eventArgs)
{
    // 服务改变时，只更新 Enable 状态到配置文件
    // 不替换整个 Services 数组，避免用 ServiceManager 内存中的旧值
    // （如 WorkingDirectory）覆盖配置文件中的新值
    var set = AgentSetting;
    var mgrServices = _Manager.Services ?? [];
    var setServices = set.Services ?? [];

    var changed = false;
    foreach (var mgrSvc in mgrServices)
    {
        var setSvc = setServices.FirstOrDefault(e => e.Name.EqualIgnoreCase(mgrSvc.Name));
        if (setSvc != null && setSvc.Enable != mgrSvc.Enable)
        {
            setSvc.Enable = mgrSvc.Enable;
            changed = true;
        }
    }

    if (changed) set.Save();
}
```

### 修复优势

- ✅ Start/Stop 只更新 Enable 字段，保留用户修改的其他参数
- ✅ 避免内存旧值覆盖配置文件新值
- ✅ 无变更时不触发不必要的 Save

---

## 📊 影响评估

| 影响项 | 说明 |
|--------|------|
| 严重程度 | 高（用户修改丢失） |
| 触发频率 | 每次 Start/Stop/Restart 服务时触发 |
| 影响范围 | 所有通过 WebPanel 修改服务参数的用户 |
| 临时规避 | 修改服务参数后不要立即 Start/Stop，先重启 StarAgent 让内存刷新 |

---

## 🔗 关联问题

- #148（StarAgentSetting.OnLoaded 覆盖用户配置）：与 P015 配合时，会反复覆盖（清空配置 → P015 填充示例 → P016 用内存旧值覆盖）

---

## 🙏 致谢

感谢 NewLife 团队的优秀开源工作！

---

**标签建议**：`bug`、`webpanel`、`data-loss`
