# StarAgent 远程命令使用指南

## 重要说明

**Stardust 客户端（AppClient）本身没有默认的远程命令**，只有以下 3 个基础命令：

| 命令 | 说明 |
|------|------|
| egistry/register | 重新注册到星尘平台 |
| egistry/unregister | 从星尘平台取消注册 |
| pp/freeMemory | 释放应用内存 |

**StarAgent 项目扩展了远程命令功能**，在 [MyStarClient.cs](file:///c:/Users/Administrator/source/repos/Stardust/StarAgent/MyStarClient.cs) 中实现了以下 6 个默认命令。

---

## 一、StarAgent 默认远程命令

### 1.1 命令列表

| 命令 | 说明 | 参数格式 | 示例 |
|------|------|----------|------|
| 
ode/restart | 重启 StarAgent 服务 | 无参数 | 
ode/restart |
| 
ode/reboot | 重启操作系统 | 无参数 | 
ode/reboot |
| 
ode/setchannel | 设置频道 | 频道名称 | 
ode/setchannel=production |
| 
ode/synctime | 同步系统时间 | 无参数 | 
ode/synctime |
| ash | 执行 Linux Bash 命令 | JSON 或纯文本 | ash={"cmd":"ls -la","timeout":30000} |
| cmd | 执行 Windows CMD 命令 | JSON 或纯文本 | cmd=dir /b |

### 1.2 命令详细说明

#### 1. node/restart - 重启 StarAgent 服务

**功能**: 重启 StarAgent 服务本身

**参数**: 无

**执行示例**:
`json
{
  "cmd": "node/restart"
}
`

**使用场景**:
- StarAgent 配置更新后需要重启
- StarAgent 出现异常需要重启

---

#### 2. node/reboot - 重启操作系统

**功能**: 重启整个操作系统

**参数**: 无

**执行示例**:
`json
{
  "cmd": "node/reboot"
}
`

**使用场景**:
- 系统更新后需要重启
- 系统出现严重问题需要重启

**注意**: 此命令会重启整个服务器，请谨慎使用！

---

#### 3. node/setchannel - 设置频道

**功能**: 切换 StarAgent 的运行频道

**参数**: 频道名称（字符串）

**执行示例**:
`json
{
  "cmd": "node/setchannel",
  "arg": "production"
}
`

**可用频道**:
- production - 生产环境
- development - 开发环境
- 	esting - 测试环境

---

#### 4. node/synctime - 同步系统时间

**功能**: 与星尘平台同步系统时间

**参数**: 无

**执行示例**:
`json
{
  "cmd": "node/synctime"
}
`

**使用场景**:
- 系统时间偏差较大时
- 定期时间校准

---

#### 5. bash - 执行 Linux Bash 命令

**功能**: 在 Linux 系统上执行 Bash 命令

**参数格式**:
- **简单模式**: 直接传入命令字符串
- **高级模式**: JSON 格式，支持超时设置

**执行示例**:

**简单模式**:
`json
{
  "cmd": "bash",
  "arg": "ls -la /home"
}
`

**高级模式**:
`json
{
  "cmd": "bash",
  "arg": "{\\"cmd\\":\\"ps aux\\",\\"timeout\\":60000}"
}
`

**返回结果**:
`
命令执行输出...
`

---

#### 6. cmd - 执行 Windows CMD 命令

**功能**: 在 Windows 系统上执行 CMD 命令

**参数格式**:
- **简单模式**: 直接传入命令字符串
- **高级模式**: JSON 格式，支持超时设置

**执行示例**:

**简单模式**:
`json
{
  "cmd": "cmd",
  "arg": "dir C:\\Program Files"
}
`

**高级模式**:
`json
{
  "cmd": "cmd",
  "arg": "{\\"cmd\\":\\"tasklist\\",\\"timeout\\":30000}"
}
`

**返回结果**:
`
命令执行输出...
`

---

## 二、添加自定义远程命令

### 2.1 扩展 MyStarClient.cs

**文件位置**: [StarAgent/MyStarClient.cs](file:///c:/Users/Administrator/source/repos/Stardust/StarAgent/MyStarClient.cs)

### 2.2 实现步骤

#### 步骤 1: 注册命令

在 MyStarClient.Open() 方法中注册新命令：

`csharp
public override void Open()
{
    // 原有的命令注册
    this.RegisterCommand("node/restart", Restart);
    this.RegisterCommand("node/reboot", Reboot);
    this.RegisterCommand("node/setchannel", SetChannel);
    this.RegisterCommand("node/synctime", SyncTime);
    this.RegisterCommand("bash", RunBash);
    this.RegisterCommand("cmd", RunCmd);
    
    // 新增：注册自定义命令
    this.RegisterCommand("custom/status", GetAppStatus);
    this.RegisterCommand("custom/restartapp", RestartAppHandler);
    this.RegisterCommand("custom/collectmetrics", CollectMetrics);
    
    base.Open();
}
`

#### 步骤 2: 实现命令处理方法

**示例 1: 获取应用状态**

`csharp
private async Task<String> GetAppStatus(String argument)
{
    try
    {
        var status = new
        {
            status = "running",
            uptime = Process.GetCurrentProcess().StartTime,
            memory = Process.GetCurrentProcess().WorkingSet64,
            timestamp = DateTime.Now
        };
        
        return status.ToJson();
    }
    catch (Exception ex)
    {
        XTrace.WriteLine("获取状态失败：{0}", ex.Message);
        return $"获取状态失败：{ex.Message}";
    }
}
`

**示例 2: 重启指定应用**

`csharp
private async Task<String> RestartAppHandler(String argument)
{
    if (argument.IsNullOrEmpty())
        return "参数为空，请指定应用名称";
    
    try
    {
        WriteLog("执行自定义命令：重启应用 {0}", argument);
        WriteEvent("warn", "RestartApp", $"重启应用：{argument}");
        
        // 查找应用控制器
        var controller = Manager?.QueryByName(argument);
        if (controller == null)
            return $"未找到应用：{argument}";
        
        // 检查应用是否已启用
        if (!controller.Enable)
            return $"应用 {argument} 未启用，无法重启";
        
        // 重启应用
        var oldPid = controller.Process?.Id ?? 0;
        controller.Stop();
        await Task.Delay(1000);
        controller.Start();
        
        // 等待应用启动
        await Task.Delay(3000);
        
        var newPid = controller.Process?.Id ?? 0;
        
        WriteLog("应用 {0} 重启成功，旧 PID: {1}, 新 PID: {2}", argument, oldPid, newPid);
        WriteEvent("info", "RestartApp", $"应用 {argument} 重启成功，PID: {newPid}");
        
        return $"应用 {argument} 重启成功（PID: {newPid}）";
    }
    catch (Exception ex)
    {
        WriteLog("重启应用失败：{0}", ex.Message);
        WriteEvent("error", "RestartApp", $"{argument} 重启失败：{ex.Message}");
        return $"{argument} 重启失败：{ex.Message}";
    }
}
`

**示例 3: 采集自定义指标**

`csharp
private async Task<String> CollectMetrics(String argument)
{
    try
    {
        var metrics = new Dictionary<String, Object>
        {
            ["cpu"] = PerformanceCounterHelper.GetCpuUsage(),
            ["memory"] = PerformanceCounterHelper.GetMemoryUsage(),
            ["disk"] = PerformanceCounterHelper.GetDiskUsage(),
            ["network"] = PerformanceCounterHelper.GetNetworkUsage(),
            ["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        
        // 上报到星尘平台
        if (Provider?.GetService<StarClient>() is StarClient client)
        {
            await client.ReportMetricsAsync(metrics);
        }
        
        return metrics.ToJson();
    }
    catch (Exception ex)
    {
        XTrace.WriteLine("采集指标失败：{0}", ex.Message);
        return $"采集指标失败：{ex.Message}";
    }
}
`

#### 步骤 3: 添加辅助方法（可选）

`csharp
// 写入日志
private void WriteLog(String message, params Object[] args)
{
    XTrace.WriteLine(message, args);
}

// 写入事件
private void WriteEvent(String category, String action, String remark)
{
    Provider?.GetService<StarClient>()?.WriteEvent(category, action, remark);
}
`

---

### 2.3 命令命名规范

**推荐格式**: 分类/动作

**示例**:
- custom/status - 自定义状态查询
- custom/restartapp - 自定义重启应用
- pp/deploy - 应用部署
- monitor/check - 监控检查
- config/update - 配置更新

---

### 2.4 完整代码示例

`csharp
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using NewLife;
using NewLife.Log;
using Stardust;

namespace StarAgent
{
    public class MyStarClient : StarClient
    {
        public ServiceBase Service { get; set; }
        public StarAgentSetting AgentSetting { get; set; }
        private Boolean InService { get; }

        public MyStarClient()
        {
            InService = Environment.UserInteractive == false;
        }

        public override void Open()
        {
            // 原有的命令注册
            this.RegisterCommand("node/restart", Restart);
            this.RegisterCommand("node/reboot", Reboot);
            this.RegisterCommand("node/setchannel", SetChannel);
            this.RegisterCommand("node/synctime", SyncTime);
            this.RegisterCommand("bash", RunBash);
            this.RegisterCommand("cmd", RunCmd);
            
            // 新增：注册自定义命令
            this.RegisterCommand("custom/status", GetAppStatus);
            this.RegisterCommand("custom/restartapp", RestartAppHandler);
            this.RegisterCommand("custom/collectmetrics", CollectMetrics);
            
            base.Open();
        }

        // 自定义命令实现
        public async Task<String> GetAppStatus(String argument)
        {
            try
            {
                var status = new
                {
                    status = "running",
                    uptime = Process.GetCurrentProcess().StartTime,
                    memory = Process.GetCurrentProcess().WorkingSet64,
                    timestamp = DateTime.Now
                };
                
                return status.ToJson();
            }
            catch (Exception ex)
            {
                XTrace.WriteLine("获取状态失败：{0}", ex.Message);
                return $"获取状态失败：{ex.Message}";
            }
        }

        public async Task<String> RestartAppHandler(String argument)
        {
            if (argument.IsNullOrEmpty())
                return "参数为空，请指定应用名称";
            
            try
            {
                WriteLog("执行自定义命令：重启应用 {0}", argument);
                WriteEvent("warn", "RestartApp", $"重启应用：{argument}");
                
                var controller = Manager?.QueryByName(argument);
                if (controller == null)
                    return $"未找到应用：{argument}";
                
                if (!controller.Enable)
                    return $"应用 {argument} 未启用，无法重启";
                
                var oldPid = controller.Process?.Id ?? 0;
                controller.Stop();
                await Task.Delay(1000);
                controller.Start();
                
                await Task.Delay(3000);
                
                var newPid = controller.Process?.Id ?? 0;
                
                WriteLog("应用 {0} 重启成功，旧 PID: {1}, 新 PID: {2}", argument, oldPid, newPid);
                WriteEvent("info", "RestartApp", $"应用 {argument} 重启成功，PID: {newPid}");
                
                return $"应用 {argument} 重启成功（PID: {newPid}）";
            }
            catch (Exception ex)
            {
                WriteLog("重启应用失败：{0}", ex.Message);
                WriteEvent("error", "RestartApp", $"{argument} 重启失败：{ex.Message}");
                return $"{argument} 重启失败：{ex.Message}";
            }
        }

        public async Task<String> CollectMetrics(String argument)
        {
            try
            {
                var metrics = new Dictionary<String, Object>
                {
                    ["cpu"] = PerformanceCounterHelper.GetCpuUsage(),
                    ["memory"] = PerformanceCounterHelper.GetMemoryUsage(),
                    ["disk"] = PerformanceCounterHelper.GetDiskUsage(),
                    ["network"] = PerformanceCounterHelper.GetNetworkUsage(),
                    ["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                
                if (Provider?.GetService<StarClient>() is StarClient client)
                {
                    await client.ReportMetricsAsync(metrics);
                }
                
                return metrics.ToJson();
            }
            catch (Exception ex)
            {
                XTrace.WriteLine("采集指标失败：{0}", ex.Message);
                return $"采集指标失败：{ex.Message}";
            }
        }

        private void WriteLog(String message, params Object[] args)
        {
            XTrace.WriteLine(message, args);
        }

        private void WriteEvent(String category, String action, String remark)
        {
            Provider?.GetService<StarClient>()?.WriteEvent(category, action, remark);
        }
    }
}
`

---

## 三、测试远程命令

### 3.1 通过星尘平台测试

1. 登录星尘平台
2. 进入节点管理
3. 选择目标节点
4. 发送远程命令

### 3.2 查看执行结果

**日志位置**:
`
Logs/staragent.log
`

**查看命令**:
`powershell
Get-Content "C:\\Users\\Administrator\\source\\repos\\Stardust\\Logs\\staragent.log" -Tail 50 -Wait
`

---

## 四、注意事项

### 4.1 安全性

1. **权限控制**: 确保只有授权用户可以发送命令
2. **命令验证**: 对命令参数进行严格验证
3. **日志记录**: 记录所有命令执行历史
4. **超时设置**: 为长时间运行的命令设置超时

### 4.2 错误处理

`csharp
try
{
    // 命令执行逻辑
}
catch (Exception ex)
{
    XTrace.WriteLine("命令执行失败：{0}", ex.Message);
    return $"命令执行失败：{ex.Message}";
}
`

### 4.3 性能考虑

1. **异步执行**: 使用 sync/await 避免阻塞
2. **资源释放**: 及时释放文件句柄、网络连接等资源
3. **限流**: 避免频繁执行消耗资源的命令

---

## 五、总结

### StarAgent vs Stardust

| 项目 | 远程命令 | 说明 |
|------|----------|------|
| **Stardust** | 3 个基础命令 | registry/register, registry/unregister, app/freeMemory |
| **StarAgent** | 6 个默认命令 + 自定义扩展 | node/*, bash, cmd + 自定义命令 |

### 最佳实践

1. **命令命名**: 使用 分类/动作 格式
2. **参数验证**: 严格验证所有输入参数
3. **错误处理**: 捕获异常并返回友好错误信息
4. **日志记录**: 记录命令执行全过程
5. **性能优化**: 使用异步编程，避免阻塞

---

**文档版本**: 1.0  
**最后更新**: 2026-04-13  
**维护者**: 科控物联
