# StarAgentSetting.OnLoaded 在 Services 为空时强制填充示例服务，覆盖用户真实配置

## 📋 问题描述

当用户清空 `StarAgent.config` 中的 `<Services>` 节点（或配置文件解析异常导致 Services 为 null）时，`StarAgentSetting.OnLoaded` 会强制填充 4 个示例服务（test/test2/StarServer/StarWeb）。后续任何调用 `Save()` 的操作（如 `LoadUser`、动态服务变更等）都会把这些示例服务持久化到配置文件，**覆盖用户的空配置或真实配置**。

---

## 🔍 复现步骤

1. 编辑 `StarAgent.config`，删除 `<Services>` 节点（或设为 `<Services />`）
2. 启动 StarAgent
3. 通过 WebPanel 启动/停止任意服务（触发 `Save()`）
4. 检查 `StarAgent.config` —— `<Services>` 被写入了 test/test2/StarServer/StarWeb 4 个示例服务

**预期行为**：用户清空 Services 后应保持空状态。
**实际行为**：每次启动都会被填充示例服务，且后续 `Save()` 会持久化覆盖。

---

## 📍 问题定位

文件：[`StarAgent/Setting.cs`](https://github.com/NewLifeX/Stardust/blob/master/StarAgent/Setting.cs) `OnLoaded` 方法

**官方代码**：

```csharp
protected override void OnLoaded()
{
    if (Services == null || Services.Length == 0)
    {
        var si = new ServiceInfo
        {
            Name = "test",
            FileName = "test.exe",
            Arguments = "-c",
            Enable = false,
        };
        var si2 = new ServiceInfo
        {
            Name = "test2",
            FileName = "dotnet",
            Arguments = "test.dll",
            Enable = false,
        };
        var si3 = new ServiceInfo
        {
            Name = "StarServer",
            FileName = "StarServer.dll",
            Enable = false,
        };
        var si4 = new ServiceInfo
        {
            Name = "StarWeb",
            FileName = "StarWeb.dll",
            Enable = false,
        };
        Services = [si, si2, si3, si4];
    }
    base.OnLoaded();
}
```

### 根本原因

`OnLoaded` 是配置加载成功的回调，不应在此处修改配置内容。当用户**主动清空** Services 时，`OnLoaded` 应该尊重用户意图，保持空状态。

当前逻辑的问题：
1. `Services.Length == 0` 判断会把"用户主动清空"和"首次创建"两种情况混淆
2. 填充示例服务后，如果配置文件被外部修改或解析异常，下一次启动会"自动恢复"示例服务而非报错
3. 与 `OnServiceChanged` 配合时，会反复写入示例服务，覆盖用户通过 WebPanel 修改的真实配置

---

## 💡 建议修复方案

仅在**首次创建**（`IsNew`）时填充示例服务，不覆盖用户清空后的空配置：

```csharp
protected override void OnLoaded()
{
    // 仅在首次创建（IsNew）时填充示例服务，尊重用户清空配置的意图
    if (Services == null && IsNew)
    {
        var si = new ServiceInfo
        {
            Name = "test",
            FileName = "test.exe",
            Arguments = "-c",
            Enable = false,
        };
        var si2 = new ServiceInfo { /* ... */ };
        var si3 = new ServiceInfo { /* ... */ };
        var si4 = new ServiceInfo { /* ... */ };
        Services = [si, si2, si3, si4];
    }
    else
    {
        // 保持用户的空配置或解析后的空数组
        Services ??= [];
    }
    base.OnLoaded();
}
```

### 修复优势

- ✅ 首次运行仍有示例服务参考
- ✅ 用户清空配置后保持空状态
- ✅ 配置文件解析异常时不会自动恢复示例服务（让用户感知到问题）
- ✅ 与 `OnServiceChanged` 配合时不会反复覆盖

---

## 📊 影响评估

| 影响项 | 说明 |
|--------|------|
| 严重程度 | 中（数据覆盖，但不丢失原数据） |
| 触发频率 | 每次启动 + 每次 Save 时触发 |
| 影响范围 | 所有清空 Services 的用户 |
| 临时规避 | 不要清空 `<Services>` 节点，至少保留一个 `Enable=false` 的空服务 |

---

## 🙏 致谢

感谢 NewLife 团队的优秀开源工作！

---

**标签建议**：`bug`、`configuration`、`data-loss`
