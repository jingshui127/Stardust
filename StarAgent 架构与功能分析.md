# StarAgent 架构与功能分析文档

## 文档信息

- **项目名称**: StarAgent（进程守护）
- **版本**: v3.7.2026.0413
- **框架**: .NET Framework 4.6.1 / .NET Core 3.1+ / .NET 5-10
- **开发团队**: 科控物联
- **文档日期**: 2026-04-13
- **参考资料**: 星尘官方文档（Doc 目录）

---

## 一、项目概述

### 1.1 产品定位

StarAgent 是一个进程守护代理程序，是星尘分布式系统的关键组件，部署于每台应用服务器或边缘节点。

**核心价值**:
- 进程守护：持续监控应用程序，自动重启崩溃、卡顿或停止的应用
- 远程发布：支持远程部署和更新应用程序，无需现场操作
- 性能监控：采集 CPU、内存、磁盘、网络、GPU 等系统指标
- 日志上报：收集应用日志并上报到星尘平台
- 服务治理：服务注册、发现、配置管理、负载均衡

### 1.2 应用场景

1. 非服务应用后台运行：让普通应用程序在后台持续运行
2. 系统启动自启：在系统启动时自动启动应用（甚至在用户登录前）
3. 应用故障恢复：检测应用异常并自动重启
4. 远程运维：远程部署、更新和管理应用
5. 分布式监控：采集和上报节点性能数据
6. 微服务治理：服务注册发现、配置中心、链路追踪

---

## 二、整体架构设计

### 2.1 架构分层

星尘平台 (StarServer) → HTTP/WebSocket → StarAgent (守护代理) → 被守护的应用进程

### 2.2 技术栈选型

**核心框架**:
- .NET Framework 4.6.1+: 主运行框架（Windows）
- .NET Core 3.1+: 跨平台支持
- .NET 5-10: 最新框架支持

**核心依赖库**:
- NewLife.Agent 10.15.x: 代理服务框架
- NewLife.Core 2026.x: 基础工具库
- NewLife.Remoting 2026.x: 远程通信框架
- Stardust 2026.x: 星尘客户端 SDK
- ILRepack 2.0.44: 程序集合并工具（仅 net461）

### 2.3 设计模式

1. 服务宿主模式：使用 IHost 接口统一不同平台的服务运行方式
2. 策略模式：部署策略（标准部署、影子部署、任务部署、托管部署）
3. 工厂模式：部署策略工厂、服务控制器工厂
4. 观察者模式：配置变更监听、性能指标上报
5. 单例模式：StarClient、ConfigManager 等核心组件

---

## 三、核心功能模块

### 3.1 模块划分

StarAgent
- 核心服务模块 (StarService): 服务生命周期管理、进程守护、看门狗机制
- 进程管理模块 (ServiceController/ServiceManager): 应用进程控制、健康检查、自动重启
- 远程部署模块 (DeployManager): 标准部署、影子部署、任务部署、托管部署、文件同步
- 监控采集模块：性能指标采集、日志采集、指标上报
- 配置管理模块：本地配置、远程配置拉取、配置热更新
- 通信模块 (StarClient): 服务注册、服务发现、RPC 通信

### 3.2 核心模块详解

#### 3.2.1 StarService（核心服务）

职责：作为 Windows 服务或 Linux 守护进程运行，统筹所有子模块

关键代码位置：StarAgent/StarService.cs

核心功能:
1. 服务初始化：加载配置、初始化 StarClient、注册服务
2. 服务启动：启动看门狗定时器、启动被守护的应用
3. 服务停止：优雅关闭所有子进程、清理资源
4. 看门狗机制：定期检查应用状态，触发健康检查和自动重启

业务逻辑流程:
服务启动 → 初始化 StarClient → 连接到星尘平台 → 加载本地配置 → 拉取远程配置 
→ 启动被守护应用 → 启动看门狗定时器 → [循环] 检查应用状态

#### 3.2.2 ServiceController（应用控制器）

职责：管理单个应用进程的生命周期

关键代码位置：Stardust/Managers/ServiceController.cs

核心功能:
1. 进程启动：启动应用进程，设置工作目录、环境变量
2. 进程监控：监听进程退出事件
3. 进程停止：优雅停止或强制终止进程
4. 健康检查：检查应用是否响应（HTTP/TCP/自定义检查）
5. 自动重启：进程异常退出后自动重启

健康检查流程:
触发健康检查 → 检查类型判断 (HTTP/TCP/进程/自定义) → 检查结果判断 
→ 健康 (重置失败计数) / 不健康 (失败计数 +1 → 超过阈值触发重启)

#### 3.2.3 部署策略模块

关键代码位置：StarAgent/Managers/

部署策略类型:

| 策略 | 说明 | 适用场景 |
|------|------|----------|
| StandardDeployStrategy | 标准部署 | 直接覆盖安装 |
| ShadowDeployStrategy | 影子部署 | 灰度发布、A/B 测试 |
| TaskStrategy | 任务部署 | 定时任务、一次性任务 |
| HostedStrategy | 托管部署 | 容器化部署 |

标准部署流程:
接收部署指令 → 下载应用包 → 解压到临时目录 → 停止旧版本 → 备份 (可选) 
→ 覆盖安装新版本 → 启动新版本 → 验证成功 → 清理临时文件

---

## 四、数据流转机制

### 4.1 配置数据流

星尘平台 (配置中心) → HTTP → StarAgent (ConfigManager) → 本地配置文件 (Star.config/Agent.config)

### 4.2 监控数据流

操作系统/硬件资源 → 定时采集 → 采集模块 (Metrics) → 数据聚合 (Aggregation) 
→ HTTP/WebSocket → 星尘平台 (监控中心)

### 4.3 服务注册与发现

StarAgent (Service A) → 注册 → 星尘平台 (Registry) 
星尘平台 (Registry) → 查询 → StarAgent (Consumer)

### 4.4 日志数据流

应用日志/系统日志 → 收集 → LogCollector → 批量上报 → 星尘平台 (日志中心)

### 4.5 远程发布数据流

星尘平台 (发布中心) → 发布指令 → StarAgent (DeployManager) 
→ 下载应用包 → 解压部署 → 启动新版本 → 验证成功 → 上报状态 → 清理旧版本

---

## 五、关键技术实现

### 5.1 进程守护机制

#### 5.1.1 看门狗定时器

核心实现逻辑：
- 定时器间隔：5 秒检查一次
- 检查进程是否存在
- 执行健康检查
- 失败计数累加，超过阈值触发重启

#### 5.1.2 优雅重启策略

1. 停止旧进程（给予宽限期 30 秒）
2. 清理残留进程
3. 启动新进程
4. 等待启动完成（10 秒）
5. 验证启动成功

#### 5.1.3 进程启动参数配置

根据官方文档 ProcessStart.md 的测试结果：

| 参数 | 说明 | 适用场景 |
|------|------|----------|
| Shell | 是否使用 Shell 执行 | Windows 服务推荐 false |
| WorkingDirectory | 工作目录 | 必须设置，避免路径问题 |
| Environment | 环境变量 | 注入 STAR_NODE、STAR_ENV 等 |
| UseShellExecute | Windows 特有 | false 可合并输出、设置环境变量 |

最佳实践:
`csharp
var startInfo = new ProcessStartInfo
{
    FileName = "MyApp.exe",
    WorkingDirectory = @"C:\Apps\MyApp",
    UseShellExecute = false, // 必须为 false
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    EnvironmentVariables =
    {
        ["STAR_NODE"] = "node-001",
        ["STAR_ENV"] = "production",
        ["STAR_VERSION"] = "1.0.0"
    }
};
`

### 5.2 远程部署实现

#### 5.2.1 文件同步算法

增量同步：只传输变化的文件
- 对比文件哈希值
- 只复制变化的文件
- 清理多余文件

#### 5.2.2 原子更新策略

1. 准备临时目录
2. 备份当前版本
3. 移动新版本到正式目录
4. 启动新版本
5. 验证成功 → 删除备份
6. 失败 → 回滚恢复旧版本

### 5.3 健康检查实现

#### 5.3.1 HTTP 健康检查
- 发送 HTTP 请求到指定 URL
- 检查响应状态码
- 超时控制

#### 5.3.2 TCP 健康检查
- 尝试连接指定端口
- 检查连接状态
- 超时控制

### 5.4 性能指标采集

#### 5.4.1 CPU 使用率采集
- Windows: PerformanceCounter
- Linux: /proc/stat

#### 5.4.2 内存使用量采集
- Windows: GetTotalPhysicalMemory/GetAvailablePhysicalMemory
- Linux: 解析/proc/meminfo

### 5.5 日志采集与上报

#### 5.5.1 日志收集
- 使用 ConcurrentQueue 队列缓存
- 达到阈值（100 条）立即上报
- 定时刷新

---

## 六、与外部系统交互

### 6.1 星尘平台 (StarServer)

#### 6.1.1 服务注册
- POST /api/registry/register
- 注册服务信息（名称、地址、版本、元数据）

#### 6.1.2 配置拉取
- GET /api/config?app=MyApp&env=production
- 获取连接字符串、应用设置等

#### 6.1.3 指标上报
- POST /api/metrics/report
- 上报 CPU、内存、磁盘、网络等指标

### 6.2 被守护的应用

#### 6.2.1 进程启动参数
`ash
MyApp.exe --agent=http://star-server:6600 --env=production
`

#### 6.2.2 环境变量注入
- STAR_NODE: 节点标识
- STAR_ENV: 环境标识
- STAR_VERSION: 应用版本

### 6.3 文件系统

#### 6.3.1 目录结构

`
StarAgent/
├── Config/              # 配置文件目录
│   ├── StarAgent.config
│   ├── Star.config
│   └── Agent.config
├── Data/                # 数据目录
│   └── machine_info.json
├── Apps/                # 被守护的应用
│   ├── MyApp/
│   │   ├── MyApp.exe
│   │   └── MyApp.config
│   └── OtherApp/
└── Logs/                # 日志目录
    └── StarAgent.log
`

---

## 七、单文件打包技术

### 7.1 ILRepack 配置

项目文件配置 (StarAgent.csproj):

`xml
<ItemGroup>
  <PackageReference Include="ILRepack" Version="2.0.44" Condition="''=='net461'">
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>
</ItemGroup>

<Import Project="ILRepack.targets" Condition="''=='net461'" />
`

### 7.2 ILRepack 构建目标

ILRepack.targets:
- 合并所有依赖 DLL 到单个 EXE
- 清理原始文件（DLL、PDB、config）
- 重命名合并后的文件

### 7.3 打包效果

打包前：约 7 MB（多个文件）
打包后：约 2 MB（单个 EXE 文件）

优势:
- ✅ 部署简单：只需复制单个 EXE 文件
- ✅ 避免 DLL 丢失问题
- ✅ 减少文件数量，便于管理
- ✅ 保护代码（内部化所有类型）

---

## 八、跨平台支持

### 8.1 Windows 平台

服务类型：Windows Service

安装方式:
`powershell
sc create StarAgent binPath= "C:\StarAgent\StarAgent.exe" start= auto
sc start StarAgent
`

### 8.2 Linux 平台

服务类型：systemd Service

服务文件：/etc/systemd/system/staragent.service

安装方式:
`ash
sudo systemctl daemon-reload
sudo systemctl enable staragent
sudo systemctl start staragent
`

### 8.3 IoT 边缘平台

运行模式：后台进程或容器

Docker 部署:
`dockerfile
FROM mcr.microsoft.com/dotnet/runtime:6.0
COPY StarAgent /app/
WORKDIR /app
ENTRYPOINT ["./StarAgent"]
`

---

## 九、安全性设计

### 9.1 认证与授权
- API 密钥认证
- OAuth 2.0 令牌
- 基于角色的访问控制 (RBAC)

### 9.2 通信安全
- HTTPS 加密
- 证书验证
- 敏感数据加密传输

### 9.3 文件安全
- 文件权限限制
- 完整性校验（哈希）
- 数字签名验证

---

## 十、性能优化

### 10.1 内存管理
- 对象池重用
- 内存池（System.Buffers）
- 异步处理避免阻塞

### 10.2 网络优化
- 连接池复用 HTTP 连接
- 批量上报合并数据
- GZip 压缩大数据

### 10.3 启动优化
- 延迟加载非关键组件
- 并行初始化独立组件
- 缓存远程配置

---

## 十一、故障处理

### 11.1 异常处理策略

`csharp
try
{
    await Operation();
}
catch (NetworkException ex)
{
    // 网络异常：重试机制
    await RetryAsync();
}
catch (ConfigurationException ex)
{
    // 配置异常：使用默认配置
    UseDefaultConfig();
}
catch (ProcessException ex)
{
    // 进程异常：重启应用
    await RestartApp();
}
catch (Exception ex)
{
    // 未知异常：记录日志并上报
    _log.Error(ex);
    await ReportError(ex);
}
`

### 11.2 重试机制
- 最大重试次数：3 次
- 指数退避：2^n 秒延迟
- 记录重试日志

### 11.3 日志记录
- 分级日志：Trace < Debug < Info < Warn < Error < Fatal
- 结构化日志：JSON 格式
- 日志轮转：按大小或时间分割
- 远程上报：关键错误上报到星尘平台

---

## 十二、监控与诊断

### 12.1 内置监控
- 健康状态实时显示
- 性能指标采集
- 进程信息（运行时间、重启次数、失败次数）

### 12.2 诊断工具
- 命令行工具：StarAgent.exe --status
- 日志查看器：实时查看日志
- 性能分析：内置性能分析工具

### 12.3 远程诊断
- 远程日志：从星尘平台查看远程日志
- 远程命令：执行远程诊断命令
- 性能追踪：全链路性能追踪

---

## 十三、最佳实践

### 13.1 部署建议

生产环境:
- 使用 Release 模式编译
- 启用 ILRepack 单文件打包
- 配置日志轮转
- 设置合理的健康检查间隔

开发环境:
- 使用 Debug 模式便于调试
- 禁用单文件打包便于分析
- 启用详细日志

### 13.2 配置建议

1. 看门狗间隔：5-10 秒（根据应用特性调整）
2. 健康检查超时：10-30 秒
3. 失败阈值：3-5 次连续失败后重启
4. 重启延迟：5-10 秒（避免频繁重启）

### 13.3 监控建议

关键指标:
- 应用在线率
- 平均重启次数
- 健康检查失败率
- 资源使用率趋势

告警规则:
- 应用连续重启 > 3 次/小时
- CPU 使用率 > 90% 持续 5 分钟
- 内存使用率 > 95%
- 健康检查失败率 > 50%

### 13.4 部署模式选择

| 场景 | 推荐模式 | 理由 |
|------|---------|------|
| 大多数应用 | Standard | 简单直接，易于理解 |
| 频繁更新的应用 | Shadow | 保持配置文件，热更新 |
| IIS/Nginx 托管 | Hosted | 仅解压，由外部宿主运行 |
| 数据库迁移工具 | Task | 运行一次后完成，不守护 |

### 13.5 滚动发布

配置节点延迟:
`
节点 A: Delay=0     （立即发布）
节点 B: Delay=60    （延迟 60 秒）
节点 C: Delay=120   （延迟 120 秒）
`

优点:
- 降低风险
- 便于及时发现问题
- 支持灰度发布

---

## 十四、故障排查

### 14.1 常见问题

#### 问题 1：发布后节点未更新

可能原因:
1. 节点未连接到服务端
2. 节点上的 StarAgent 未运行
3. WebSocket 连接断开，HTTP 轮询未触发

排查步骤:
1. 检查节点状态：LastActive 字段
2. 检查 StarAgent 日志：logs/staragent.log
3. 检查服务端日志：发布指令是否下发
4. 手动触发：/node/getDeploy 接口

#### 问题 2：进程启动失败

可能原因:
1. 启动文件不正确（FileName 字段）
2. 工作目录不存在（WorkingDirectory 字段）
3. 权限不足（UserName 字段）
4. 端口被占用（Port 字段）

排查步骤:
1. 检查 StarAgent 日志：启动失败原因
2. 检查应用日志：工作目录/logs
3. 检查进程状态：ProcessId 字段
4. 手动启动：验证启动命令

### 14.2 日志位置

| 组件 | 日志位置 |
|------|---------|
| Stardust.Web | Logs/*.log |
| Stardust.Server | Logs/*.log |
| StarAgent | Logs/staragent.log |
| 应用日志 | 工作目录/logs/*.log |

---

## 十五、总结

StarAgent 是一个功能强大、设计优雅的进程守护代理程序，具有以下核心优势：

### 核心优势

1. 高可靠性：看门狗机制、健康检查、自动重启
2. 易于部署：单文件打包、跨平台支持
3. 远程管理：远程部署、配置热更新、监控上报
4. 灵活扩展：多种部署策略、插件化设计
5. 性能优异：基于 NewLife.Core 的优化积累

### 适用场景

- ✅ 需要持续运行的应用程序
- ✅ 需要自动故障恢复的系统
- ✅ 分布式服务的节点管理
- ✅ IoT 边缘计算节点
- ✅ 微服务架构的服务治理

### 技术价值

StarAgent 的设计模式和实现细节为工业级应用开发提供了优秀参考：
- 服务宿主模式实现跨平台支持
- 策略模式实现灵活的部署方案
- 观察者模式实现配置热更新
- 批量上报和连接池优化网络性能
- 原子更新和回滚机制保证部署安全

---

## 附录

### A. 配置文件示例

StarAgent.config:
`xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <StarAgent>
    <!-- 服务名称 -->
    <ServiceName>MyApp</ServiceName>
    
    <!-- 应用路径 -->
    <AppPath>Apps/MyApp/MyApp.exe</AppPath>
    
    <!-- 工作目录 -->
    <WorkDir>Apps/MyApp</WorkDir>
    
    <!-- 启动参数 -->
    <Args>--env=production</Args>
    
    <!-- 健康检查配置 -->
    <HealthCheck>
      <Enabled>true</Enabled>
      <Type>Http</Type>
      <Url>http://localhost:5000/health</Url>
      <Interval>30</Interval>
      <Timeout>10</Timeout>
      <FailureThreshold>3</FailureThreshold>
    </HealthCheck>
    
    <!-- 重启配置 -->
    <Restart>
      <Enabled>true</Enabled>
      <Delay>5</Delay>
      <MaxRetries>3</MaxRetries>
    </Restart>
  </StarAgent>
</configuration>
`

### B. 常用命令

`ash
# 查看状态
StarAgent.exe --status

# 查看版本
StarAgent.exe --version

# 查看帮助
StarAgent.exe --help

# 安装为服务（Windows）
StarAgent.exe --install

# 卸载服务（Windows）
StarAgent.exe --uninstall

# 启动应用
StarAgent.exe --start MyApp

# 停止应用
StarAgent.exe --stop MyApp

# 重启应用
StarAgent.exe --restart MyApp
`

### C. 参考资料

- 星尘平台文档：http://star.newlifex.com
- NewLife.Core 文档：https://newlifex.com
- .NET 官方文档：https://docs.microsoft.com/dotnet
- ILRepack GitHub：https://github.com/gluck/ilrepack
- 官方文档目录：C:\Users\Administrator\source\repos\Stardust\Doc\

---

文档版本：1.0
最后更新：2026-04-13
维护者：科控物联
