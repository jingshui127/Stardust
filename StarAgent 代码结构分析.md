# StarAgent 完整代码结构分析

## 文档目的

本文档详细分析 StarAgent 应用程序的完整代码结构，帮助开发者理解系统架构，便于后续功能扩展和定制开发。

---

## 一、项目文件结构

### 1.1 目录结构

```
StarAgent/
├── StarAgent.csproj          # 项目文件（多目标框架）
├── ILRepack.targets          # ILRepack 打包配置（仅 net461）
├── appsettings.json          # 应用配置（可选）
├── Program.cs                # 程序入口（638 行）
├── StarService.cs            # 核心服务实现（540 行）
├── MyStarClient.cs           # 自定义 StarClient（413 行）
├── Setting.cs                # 配置设置类（4.7 KB）
├── AliyunDnsClient.cs        # 阿里云 DNS 客户端（14.4 KB）
└── AliyunDnsSetting.cs       # 阿里云 DNS 配置（1.7 KB）
```

### 1.2 项目配置（StarAgent.csproj）

**目标框架**:
```xml
<TargetFrameworks>net45;net461;netcoreapp3.1;net5.0;net6.0;net7.0;net8.0;net9.0;net10.0</TargetFrameworks>
```

**核心依赖**:
- NewLife.Agent 10.15.x - 代理服务框架
- NewLife.Core 2026.x - 基础工具库
- NewLife.Remoting 2026.x - 远程通信框架
- Stardust 2026.x - 星尘客户端 SDK
- ILRepack 2.0.44 - 程序集合并工具（仅 net461）

---

## 二、核心代码模块详解

### 2.1 Program.cs（程序入口，638 行）

**职责**: 应用程序启动、配置初始化、服务注册

**核心功能**:

#### 1. 应用启动入口
```csharp
public static async Task<int> Main(string[] args)
```

#### 2. 初始化流程
```
Main 入口
  ↓
BuildWebHost(args) - 构建 Web 主机
  ↓
CreateHostBuilder() - 配置主机
  ↓
ConfigureServices() - 注册服务
  ↓
RunAsync() - 运行主机
```

#### 3. 关键代码段

**3.1 主机创建**
```csharp
public static IHost BuildWebHost(string[] args)
{
    // 创建主机构建器
    var builder = Host.CreateDefaultBuilder(args);
    
    // 配置服务
    builder.ConfigureServices((context, services) =>
    {
        // 注册 StarService
        services.AddSingleton<StarService>();
        
        // 注册 StarClient
        services.AddSingleton<StarClient>();
        
        // 其他服务注册
    });
    
    return builder.Build();
}
```

**3.2 命令行参数处理**
- 支持 -s 参数（服务模式）
- 支持 --install 安装服务
- 支持 --uninstall 卸载服务
- 支持 --start/--stop/--restart 控制命令

**3.3 配置加载**
- 读取 appsettings.json
- 读取 Star.config
- 读取 StarAgent.config
- 环境变量注入

---

### 2.2 StarService.cs（核心服务，540 行）

**职责**: Windows Service / Linux systemd 服务实现，统筹所有子模块

**继承关系**:
```
StarService : ServiceBase<StarService, StarAgentSetting>
            : IService
```

**核心功能**:

#### 1. 属性定义
```csharp
public StarSetting StarSetting { get; set; }        // 星尘设置
public StarAgentSetting AgentSetting { get; set; }  // 代理设置
private AgentInfo _agentInfo;                        // 代理信息
private TimerX _timer;                               // 定时刷新器
```

#### 2. 构造函数
```csharp
public StarService()
{
    // 初始化定时刷新器（100ms 延迟，5 秒间隔）
    _timer = new TimerX(DoRefreshLocal, null, 100, 5_000) { Async = true };
}
```

#### 3. 核心 API 方法

**3.1 Info - 节点信息上报**
```csharp
[Api(nameof(Info))]
public AgentInfo Info(AgentInfo info)
{
    // 获取本地代理信息
    var ai = _agentInfo ??= AgentInfo.GetLocal(true);
    
    // 填充服务器地址、服务列表、代码、IP 等
    ai.Server = set.Server;
    ai.Services = Manager?.Services?.Select(e => e.Name).ToArray();
    ai.Code = AgentSetting.Code;
    ai.IP = AgentInfo.GetIps();
    
    // 更新应用服务（标记为星尘应用）
    var controller = Manager?.QueryByProcess(info.ProcessId);
    if (controller != null)
    {
        controller.IsStarApp = true;
    }
    
    return ai;
}
```

**3.2 DoRefreshLocal - 定时刷新本地信息**
```csharp
private async void DoRefreshLocal(Object state)
{
    // 定时刷新本地节点信息
    // 包括：进程列表、性能指标、服务状态等
}
```

#### 4. 服务生命周期

**4.1 启动流程**
```
OnStart()
  ↓
加载配置
  ↓
初始化 StarClient
  ↓
连接到星尘平台
  ↓
启动被守护应用
  ↓
启动看门狗定时器
```

**4.2 停止流程**
```
OnStop()
  ↓
停止看门狗定时器
  ↓
优雅关闭所有子进程
  ↓
断开星尘平台连接
  ↓
清理资源
```

#### 5. 依赖注入
```csharp
public IObjectProvider Provider { get; set; }
public ServiceManager Manager { get; set; }
```

---

### 2.3 MyStarClient.cs（自定义客户端，413 行）

**职责**: 扩展 StarClient 功能，实现自定义业务逻辑

**继承关系**:
```
MyStarClient : StarClient
             : ApiClient
```

**核心功能**:

#### 1. 属性定义
```csharp
public ServiceBase Service { get; set; }           // 关联的服务
public StarAgentSetting AgentSetting { get; set; } // 代理设置
private Boolean InService { get; }                 // 是否服务模式
```

#### 2. 初始化
```csharp
protected override void OnInit()
{
    var provider = ServiceProvider ??= ObjectContainer.Provider;
    
    // 注册默认的模型实现
    var container = ModelExtension.GetService<IObjectContainer>(provider);
    if (container != null)
    {
        container.AddTransient<IPingResponse, MyPingResponse>();
    }
    
    base.OnInit();
}
```

#### 3. 命令注册
```csharp
public override void Open()
{
    // 注册远程命令处理
    this.RegisterCommand("node/restart", Restart);      // 重启节点
    this.RegisterCommand("node/reboot", Reboot);        // 重启系统
    this.RegisterCommand("node/setchannel", SetChannel);// 设置频道
    this.RegisterCommand("node/synctime", SyncTime);    // 同步时间
    this.RegisterCommand("bash", RunBash);              // 执行 Bash
    this.RegisterCommand("cmd", RunCmd);                // 执行 CMD
    
    base.Open();
}
```

#### 4. 登录请求构建
```csharp
public override ILoginRequest BuildLoginRequest()
{
    var request = base.BuildLoginRequest();
    if (request is LoginInfo req)
    {
        req.Project = set.Project;  // 设置项目
        
        // 设置节点信息（DPI、分辨率等）
        if (info != null && InService)
        {
            if (!set.Dpi.IsNullOrEmpty()) info.Dpi = set.Dpi;
            if (!set.Resolution.IsNullOrEmpty()) info.Resolution = set.Resolution;
        }
    }
    
    return request;
}
```

#### 5. 心跳处理
```csharp
protected override async Task OnPing(Object state)
{
    await base.OnPing(state);
    
    // 时间同步（可选）
    var syncTime = AgentSetting.SyncTime;
    if (syncTime > 0 && Span.TotalMilliseconds != 0)
    {
        // 执行时间同步
    }
}
```

#### 6. 远程命令实现

**6.1 重启节点**
```csharp
private async Task Restart(Object state)
{
    // 重启 StarAgent 服务
}
```

**6.2 重启系统**
```csharp
private async Task Reboot(Object state)
{
    // 重启操作系统
}
```

**6.3 执行命令**
```csharp
private async Task RunCmd(Object state)
{
    // 执行 Windows CMD 命令
}

private async Task RunBash(Object state)
{
    // 执行 Linux Bash 命令
}
```

---

### 2.4 Setting.cs（配置设置）

**职责**: 定义配置项的强类型类

**核心配置类**:

#### 1. StarAgentSetting
```csharp
public class StarAgentSetting : Config<StarAgentSetting>
{
    public String Code { get; set; }           // 节点编码
    public String Project { get; set; }        // 项目名称
    public String Dpi { get; set; }            // DPI 设置
    public String Resolution { get; set; }     // 分辨率
    public Int32 SyncTime { get; set; }        // 时间同步间隔
    // ... 其他配置
}
```

#### 2. 配置加载
```csharp
// 从配置文件加载
var setting = StarAgentSetting.Current;

// 从环境变量加载
setting.Code = Environment.GetEnvironmentVariable("STAR_CODE");
```

---

### 2.5 AliyunDnsClient.cs（阿里云 DNS 客户端）

**职责**: 集成阿里云 DNS API，实现动态 DNS 更新

**核心功能**:

#### 1. DNS 记录管理
- 添加 DNS 记录
- 删除 DNS 记录
- 更新 DNS 记录
- 查询 DNS 记录

#### 2. 动态 DNS 更新
```csharp
// 检测 IP 变化
// 自动更新 DNS 记录
// 支持 A 记录、AAAA 记录
```

#### 3. 配置项
```csharp
public class AliyunDnsSetting
{
    public String AccessKeyId { get; set; }     // 访问密钥 ID
    public String AccessKeySecret { get; set; } // 访问密钥 Secret
    public String DomainName { get; set; }      // 域名
    public String RR { get; set; }              // 主机记录
}
```

---

## 三、模块依赖关系

### 3.1 依赖图

```
┌─────────────────────────────────────────────────────────┐
│                    Program.cs                           │
│                  (程序入口/主机)                        │
└────────────────────┬────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│                   StarService                           │
│                (核心服务/守护进程)                       │
├─────────────────────────────────────────────────────────┤
│  - TimerX（定时刷新）                                   │
│  - ServiceManager（服务管理）                           │
│  - StarClient（星尘客户端）                             │
└────────────────────┬────────────────────────────────────┘
                     │
         ┌───────────┼───────────┐
         ▼           ▼           ▼
┌─────────────┐ ┌─────────────┐ ┌─────────────┐
│  MyStarClient│ │ ServiceManager│ │  StarSetting │
│ (自定义客户端)│ │  (服务管理器) │ │  (配置管理) │
└─────────────┘ └─────────────┘ └─────────────┘
         │
         ▼
┌─────────────────┐
│ AliyunDnsClient │
│  (DNS 客户端)     │
└─────────────────┘
```

### 3.2 依赖注入容器

**注册的服务**:
```csharp
services.AddSingleton<StarService>();
services.AddSingleton<StarClient>();
services.AddSingleton<ServiceManager>();
services.AddSingleton<StarAgentSetting>();
services.AddSingleton<StarSetting>();
```

**服务解析**:
```csharp
var service = provider.GetRequiredService<StarService>();
var client = provider.GetRequiredService<StarClient>();
```

---

## 四、数据模型

### 4.1 核心数据模型

#### AgentInfo（代理信息）
```csharp
public class AgentInfo
{
    public String Name { get; set; }         // 节点名称
    public String Code { get; set; }         // 节点编码
    public String Server { get; set; }       // 服务器地址
    public String[] Services { get; set; }   // 服务列表
    public String IP { get; set; }           // IP 地址
    public String ProcessId { get; set; }    // 进程 ID
    // ... 其他属性
}
```

#### ServiceController（服务控制器）
```csharp
public class ServiceController
{
    public String Name { get; set; }         // 服务名称
    public String FileName { get; set; }     // 启动文件
    public String WorkingDirectory { get; set; } // 工作目录
    public String Args { get; set; }         // 启动参数
    public Boolean Enable { get; set; }      // 是否启用
    public Boolean IsStarApp { get; set; }   // 是否星尘应用
    public Process Process { get; set; }     // 关联进程
    // ... 其他属性
}
```

---

## 五、配置系统

### 5.1 配置文件层次

```
appsettings.json          # 应用基础配置
    ↓
Star.config              # 星尘平台配置
    ↓
StarAgent.config         # 代理守护配置
    ↓
环境变量                 # 覆盖配置
```

### 5.2 Star.config 示例
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <Star>
    <Server>http://47.113.219.65:6600</Server>
    <Name>Server-A-Node1</Name>
    <Environment>production</Environment>
    <Secret>api-secret</Secret>
  </Star>
</configuration>
```

### 5.3 StarAgent.config 示例
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <StarAgent>
    <Code>agent-001</Code>
    <Project>MyProject</Project>
    <SyncTime>3600</SyncTime>
    
    <Services>
      <Service>
        <Name>APP1</Name>
        <FileName>Apps/APP1/APP1.exe</FileName>
        <WorkingDirectory>Apps/APP1</WorkingDirectory>
        <Args>--env=production</Args>
      </Service>
    </Services>
  </StarAgent>
</configuration>
```

---

## 六、扩展点分析

### 6.1 可扩展位置

#### 1. Program.cs - 服务注册
```csharp
// 添加自定义服务
services.AddSingleton<ICustomService, CustomService>();
```

#### 2. StarService.cs - 业务逻辑
```csharp
// 扩展 Info 方法
public override AgentInfo Info(AgentInfo info)
{
    var result = base.Info(info);
    
    // 添加自定义信息
    result.Extensions["custom"] = "value";
    
    return result;
}
```

#### 3. MyStarClient.cs - 远程命令
```csharp
public override void Open()
{
    base.Open();
    
    // 注册自定义命令
    this.RegisterCommand("custom/command", CustomHandler);
}
```

#### 4. 自定义 ServiceController
```csharp
// 继承 ServiceController
public class MyServiceController : ServiceController
{
    // 扩展健康检查逻辑
    protected override Boolean CheckHealth()
    {
        var result = base.CheckHealth();
        
        // 添加自定义检查
        result &= CheckCustomHealth();
        
        return result;
    }
}
```

### 6.2 功能扩展示例

#### 示例 1：添加自定义指标采集
```csharp
// 在 StarService 中添加
private void CollectCustomMetrics()
{
    var metrics = new Dictionary<String, Object>
    {
        ["custom_metric_1"] = GetValue1(),
        ["custom_metric_2"] = GetValue2(),
    };
    
    StarClient?.ReportMetricsAsync(metrics);
}
```

#### 示例 2：添加自定义健康检查
```csharp
// 创建自定义健康检查器
public class CustomHealthChecker : IHealthChecker
{
    public Boolean Check(ServiceController controller)
    {
        // 实现自定义检查逻辑
        return true;
    }
}
```

#### 示例 3：添加告警规则
```csharp
// 在 StarService 中添加告警检查
private void CheckAlerts()
{
    if (cpuUsage > 90)
    {
        StarClient?.WriteEvent("alert", "CPU 过高", $"CPU: {cpuUsage}%");
    }
    
    if (memoryUsage > 95)
    {
        StarClient?.WriteEvent("alert", "内存过高", $"Memory: {memoryUsage}%");
    }
}
```

---

## 七、关键流程详解

### 7.1 启动流程

```
1. Program.Main()
   ↓
2. BuildWebHost() - 构建主机
   ↓
3. ConfigureServices() - 注册服务
   ├─ StarService
   ├─ StarClient
   └─ ServiceManager
   ↓
4. RunAsync() - 运行主机
   ↓
5. StarService.OnStart()
   ├─ 加载配置
   ├─ 初始化 StarClient
   ├─ 连接星尘平台
   ├─ 启动被守护应用
   └─ 启动看门狗
   ↓
6. 正常运行
```

### 7.2 心跳流程

```
1. Timer 触发（30 秒间隔）
   ↓
2. StarClient.OnPing()
   ├─ 构建心跳包
   ├─ 发送到星尘平台
   └─ 接收响应
   ↓
3. 处理响应
   ├─ 更新配置（如有变更）
   ├─ 执行远程命令（如有）
   └─ 上报指标
```

### 7.3 守护流程

```
1. Timer 触发（5 秒间隔）
   ↓
2. StarService.DoRefreshLocal()
   ├─ 检查进程是否存在
   ├─ 执行健康检查
   ├─ 失败计数累加
   └─ 超过阈值 → 重启
   ↓
3. 重启流程
   ├─ 停止旧进程
   ├─ 清理残留
   ├─ 启动新进程
   └─ 验证启动成功
```

### 7.4 部署流程

```
1. 接收部署指令
   ↓
2. DeployManager 处理
   ├─ 下载应用包
   ├─ 解压到临时目录
   ├─ 停止旧版本
   ├─ 覆盖安装
   ├─ 启动新版本
   └─ 验证成功
   ↓
3. 上报部署结果
```

---

## 八、添加功能指南

### 8.1 添加新的监控指标

**步骤 1**: 在 StarService 中添加采集方法
```csharp
private async Task CollectCustomMetrics()
{
    var metrics = new Dictionary<String, Object>();
    
    // 采集自定义指标
    metrics["my_metric"] = GetValue();
    
    // 上报
    if (Provider?.GetService<StarClient>() is StarClient client)
    {
        await client.ReportMetricsAsync(metrics);
    }
}
```

**步骤 2**: 在定时器中调用
```csharp
_timer = new TimerX(DoRefreshLocal, null, 100, 5_000) { Async = true };

private async void DoRefreshLocal(Object state)
{
    // 原有逻辑
    // ...
    
    // 新增：采集自定义指标
    await CollectCustomMetrics();
}
```

### 8.2 添加远程命令

**步骤 1**: 在 MyStarClient.Open() 中注册
```csharp
public override void Open()
{
    base.Open();
    
    // 注册新命令
    this.RegisterCommand("custom/action", CustomAction);
}
```

**步骤 2**: 实现命令处理
```csharp
private async Task CustomAction(Object state)
{
    var cmd = state as String;
    
    // 执行自定义逻辑
    XTrace.WriteLine("执行自定义命令：{0}", cmd);
    
    // 返回结果
    return new { success = true, message = "执行成功" };
}
```

### 8.3 添加新的部署策略

**步骤 1**: 创建策略类
```csharp
public class CustomDeployStrategy : IDeployStrategy
{
    public String Name => "Custom";
    
    public async Task Deploy(DeployContext context)
    {
        // 实现自定义部署逻辑
    }
}
```

**步骤 2**: 注册策略
```csharp
// 在 Program.cs 中
services.AddSingleton<IDeployStrategy, CustomDeployStrategy>();
```

### 8.4 添加日志采集器

**步骤 1**: 创建日志采集器
```csharp
public class CustomLogCollector : ILogCollector
{
    public void Collect(String logFile)
    {
        // 读取日志文件
        var logs = File.ReadAllLines(logFile);
        
        // 处理并上报
        foreach (var log in logs)
        {
            // 处理逻辑
        }
    }
}
```

**步骤 2**: 注册并使用
```csharp
services.AddSingleton<ILogCollector, CustomLogCollector>();
```

---

## 九、调试与测试

### 9.1 本地调试

**步骤 1**: 配置调试参数
```
启动配置：StarAgent
命令行参数：-console
工作目录：C:\Users\Administrator\source\repos\Stardust\StarAgent
```

**步骤 2**: 设置断点
- Program.Main() - 程序入口
- StarService.OnStart() - 服务启动
- MyStarClient.Open() - 客户端打开

### 9.2 日志查看

**日志位置**:
```
Logs/staragent.log      # StarAgent 日志
Logs/stardust.log       # 星尘 SDK 日志
```

**日志级别**:
```xml
<Log>
  <Level>Debug</Level>  <!-- Trace < Debug < Info < Warn < Error -->
</Log>
```

### 9.3 单元测试

**测试项目结构**:
```
StarAgent.Test/
├── StarServiceTest.cs
├── MyStarClientTest.cs
└── SettingTest.cs
```

**测试示例**:
```csharp
[Fact]
public void Test_Info()
{
    var service = new StarService();
    var result = service.Info(null);
    
    Assert.NotNull(result);
    Assert.NotNull(result.Name);
}
```

---

## 十、性能优化建议

### 10.1 内存优化

**1. 对象池重用**
```csharp
private static readonly ObjectPool<AgentInfo> _pool = 
    new ObjectPool<AgentInfo>(() => new AgentInfo());

var info = _pool.Get();
try
{
    // 使用 info
}
finally
{
    _pool.Return(info);
}
```

**2. 减少分配**
```csharp
// 避免：频繁创建字典
var dict = new Dictionary<String, Object>();

// 推荐：重用字典
dict.Clear();
dict["key"] = value;
```

### 10.2 网络优化

**1. 批量上报**
```csharp
// 累积一定数量后上报
private readonly Queue<Metric> _queue = new Queue<Metric>();

private void AddMetric(Metric metric)
{
    _queue.Enqueue(metric);
    
    if (_queue.Count >= 100)
    {
        FlushMetrics();
    }
}
```

**2. 连接复用**
```csharp
// StarClient 已实现连接池
// 无需额外配置
```

### 10.3 CPU 优化

**1. 异步处理**
```csharp
// 避免阻塞
await Task.Run(() => LongRunningOperation());

// 使用异步 API
await client.ReportMetricsAsync(metrics);
```

**2. 减少轮询**
```csharp
// 使用事件驱动代替轮询
// 使用 WebSocket 推送代替 HTTP 轮询
```

---

## 十一、常见问题

### Q1: 如何添加新的配置项？

**A**: 
1. 在 Setting.cs 中添加属性
2. 在配置文件中添加对应节点
3. 在代码中使用 AgentSetting.NewProperty 访问

### Q2: 如何修改心跳间隔？

**A**:
修改 Star.config:
```xml
<Star>
  <HeartbeatInterval>60</HeartbeatInterval> <!-- 秒 -->
</Star>
```

### Q3: 如何添加自定义事件？

**A**:
```csharp
StarClient?.WriteEvent("custom", "事件类型", "事件详情");
```

### Q4: 如何调试远程命令？

**A**:
1. 在命令处理函数中设置断点
2. 从星尘平台发送命令
3. 查看日志确认命令接收

---

## 十二、总结

### StarAgent 架构特点

1. **清晰的分层**: Program → StarService → StarClient
2. **依赖注入**: 使用 .NET Core DI 容器
3. **可扩展性**: 多个扩展点支持功能定制
4. **跨平台**: 支持 Windows/Linux/macOS
5. **高性能**: 基于 NewLife.Core 的优化

### 开发建议

1. **理解依赖注入**: 熟悉 .NET Core DI 模式
2. **掌握异步编程**: 大量使用 async/await
3. **阅读源码**: 深入理解 StarService 和 MyStarClient
4. **小步测试**: 每次修改后充分测试
5. **查看日志**: 善用日志调试问题

### 扩展方向

1. **监控增强**: 添加更多系统指标、业务指标
2. **告警系统**: 实现本地告警规则引擎
3. **插件系统**: 支持动态加载插件
4. **Web 控制台**: 添加本地 Web 管理界面
5. **容器支持**: 增强 Docker/K8s 集成

---

**文档版本**: 1.0  
**最后更新**: 2026-04-13  
**维护者**: 科控物联

