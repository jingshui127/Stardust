# NodeService.SendCommand 在 AllowControlNodes 为空时直接拒绝，导致内部环境无法控制节点

## 📋 问题描述

`NodeService.SendCommand` 方法中，当 `App.AllowControlNodes` 为空时，直接抛出 `ApiCode.Unauthorized` "无权操作！" 异常。这导致在**内部环境**或**兼容旧数据**的场景下，管理员无法通过节点在线页面下发控制指令（如"重启服务"）。

当前逻辑：
```csharp
if (app == null || app.AllowControlNodes.IsNullOrEmpty()) throw new ApiException(ApiCode.Unauthorized, "无权操作！");
```

`app == null` 或 `AllowControlNodes` 为空就拒绝，没有区分"首次使用"和"明确禁用"两种情况。

---

## 🔍 复现步骤

1. 部署 Stardust.Server，新建一个 App（不设置 AllowControlNodes 字段，默认为空）
2. 启动 StarAgent 连接到 Server，节点上线
3. 在节点在线页面点击"重启服务"
4. **报错**：`无权操作！`

**预期行为**：内部环境下，AllowControlNodes 为空时应默认放行（兼容旧数据/内部环境），或通过全局开关控制。
**实际行为**：直接拒绝，无法操作。

---

## 📍 问题定位

文件：[`Stardust.Server/Services/NodeService.cs`](https://github.com/NewLifeX/Stardust/blob/master/Stardust.Server/Services/NodeService.cs) `SendCommand` 方法

**官方代码**：

```csharp
var app = App.FindByName(jwt?.Subject);

if (app == null || app.AllowControlNodes.IsNullOrEmpty()) throw new ApiException(ApiCode.Unauthorized, "无权操作！");

if (app.AllowControlNodes != "*" && !node.Code.EqualIgnoreCase(app.AllowControlNodes.Split(",")))
    throw new ApiException(ApiCode.Forbidden, $"[{app}]无权操作节点[{node}]！\n安全设计需要，默认禁止所有应用向任意节点发送控制指令。\n可在注册中心应用系统中修改[{app}]的可控节点，添加[{node.Code}]，或者设置为*所有节点。");
```

### 根本原因

- `AllowControlNodes` 字段默认为空，但被当作"明确禁用"处理
- 旧数据迁移时，App 的 `AllowControlNodes` 字段可能为空，但实际应允许控制
- 内部环境通常不需要严格的节点控制权限

---

## 💡 建议修复方案

`AllowControlNodes` 为空时默认放行（兼容旧数据/内部环境），设置为具体节点或 "*" 时按原逻辑校验：

```csharp
var app = App.FindByName(jwt?.Subject);

// 内部使用：AllowControlNodes 为空时默认放行（兼容旧数据/内部环境），设置为"*"放行所有节点
if (app != null && !app.AllowControlNodes.IsNullOrEmpty() && app.AllowControlNodes != "*" &&
    !node.Code.EqualIgnoreCase(app.AllowControlNodes.Split(",")))
    throw new ApiException(ApiCode.Forbidden, $"[{app}]无权操作节点[{node}]！\n安全设计需要，默认禁止所有应用向任意节点发送控制指令。\n可在注册中心应用系统中修改[{app}]的可控节点，添加[{node.Code}]，或者设置为*所有节点。");
```

### 修复优势

- ✅ AllowControlNodes 为空时放行（兼容旧数据/内部环境）
- ✅ AllowControlNodes="*" 放行所有节点（原逻辑保留）
- ✅ AllowControlNodes=具体节点列表时按原逻辑校验
- ✅ 需要禁用时，可显式设置为不包含目标节点的列表（如 "none"）

### 备选方案（更安全）

如果担心安全风险，可增加全局开关 `StarServerSetting.AllowControlNodesWhenEmpty`（默认 true，内部环境用）：

```csharp
if (app == null || app.AllowControlNodes.IsNullOrEmpty())
{
    if (!_setting.AllowControlNodesWhenEmpty)
        throw new ApiException(ApiCode.Unauthorized, "无权操作！");
}
else if (app.AllowControlNodes != "*" && !node.Code.EqualIgnoreCase(app.AllowControlNodes.Split(",")))
{
    throw new ApiException(ApiCode.Forbidden, $"[{app}]无权操作节点[{node}]！...");
}
```

---

## 📊 影响评估

| 影响项 | 说明 |
|--------|------|
| 严重程度 | 中（功能不可用，但有安全考虑） |
| 触发频率 | 每次节点控制操作 |
| 影响范围 | 所有未设置 AllowControlNodes 的 App |
| 临时规避 | 手动为每个 App 设置 `AllowControlNodes=*` |

---

## 🙏 致谢

感谢 NewLife 团队的优秀开源工作！安全设计值得肯定，本建议旨在平衡安全与易用性。

---

**标签建议**：`bug`、`permission`、`server`
