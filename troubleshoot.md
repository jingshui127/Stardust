# StarAgent 运行问题排查

## 问题 1：端口 5580 被占用

**错误信息：**
```
System.Exception: Failed to listen to all ports! Port=[5580]
```

**原因：**
- 该电脑上已经有另一个程序占用了 5580 端口
- 可能是另一个 StarAgent 实例正在运行

**解决方案：**

### 方法 1：检查并终止占用端口的进程

```powershell
# 查看占用 5580 端口的进程
netstat -ano | findstr :5580

# 或者使用 PowerShell
Get-NetTCPConnection -LocalPort 5580 -ErrorAction SilentlyContinue | Select-Object OwningProcess

# 终止进程（替换 PID 为实际进程ID）
taskkill /F /PID <进程ID>
```

### 方法 2：修改 StarAgent 端口

编辑配置文件 `StarAgent.setting.xml`（或 `StarAgent.setting.json`）：

```xml
<?xml version="1.0" encoding="utf-8"?>
<StarAgent>
  <LocalPort>5590</LocalPort>  <!-- 改为其他端口 -->
</StarAgent>
```

或 JSON 格式：

```json
{
  "LocalPort": 5590
}
```

---

## 问题 2：无法连接到星尘服务器

**错误信息：**
```
[App]登录失败：[App/Login]已取消一个任务。
[Node]登录失败：[Node/Login]已取消一个任务。
```

**原因：**
- 网络无法访问服务器 `http://47.113.219.65:6600`
- 服务器可能未运行或防火墙阻止连接

**解决方案：**

### 检查网络连接

```powershell
# 测试服务器连通性
Test-NetConnection -ComputerName 47.113.219.65 -Port 6600

# 或使用 curl
curl http://47.113.219.65:6600
```

### 检查服务器状态

确保星尘服务器正在运行且端口 6600 已开放。

---

## 完整解决步骤

1. **先终止占用端口的进程**
2. **删除旧配置文件**（让程序重新生成默认配置）
   ```powershell
   del StarAgent.setting.*
   del StarAgent.*
   ```
3. **重新启动 StarAgent**

---

## 配置文件位置

配置文件位于 StarAgent.exe 同目录下：
- `StarAgent.setting.xml`
- `StarAgent.setting.json`
- `Star.config`