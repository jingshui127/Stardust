#if !NET40
using System.ComponentModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using NewLife;
using NewLife.Agent;
using NewLife.Agent.Models;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Serialization;
using Stardust.Managers;
using Stardust.Models;

namespace StarAgent.WebPanel;

/// <summary>StarAgent Web API 控制器（完全自建，实现 IHttpController）</summary>
/// <remarks>
/// 提供完整 RESTful API：鉴权、状态监控、服务控制、配置管理、日志查看、关于页面。
/// 路由格式：/api/{MethodName}，由 HttpServer.ControllerHandler 自动映射。
/// </remarks>
public class StarAgentApiController : IHttpController
{
    #region P/Invoke - 获取物理内存总量
    [DllImport("kernel32.dll")]
    static extern void GetPhysicallyInstalledSystemMemory(out long totalMemoryInKilobytes);
    #endregion

    #region 属性
    /// <summary>所属服务</summary>
    public ServiceBase Service => StarAgentWebPanel.Current?.Service!;

    /// <summary>当前Http上下文。由 ControllerHandler 自动注入</summary>
    public IHttpContext? Context { get; set; }

    /// <summary>获取 MyService 实例（用于访问 AgentSetting 和 ServiceManager）</summary>
    private Object? MyService => Service;
    #endregion

    #region 鉴权
    /// <summary>登录鉴权，签发Bearer Token</summary>
    public Object Login(String user, String password)
    {
        var token = StarAgentWebPanel.Current?.IssueToken(user, password);
        if (token == null) return new { code = 401, message = "Invalid credentials" };
        return new { code = 0, data = new { token } };
    }

    /// <summary>检查请求鉴权</summary>
    private Boolean CheckAuth()
    {
        var ctx = Context;
        if (ctx == null) return false;

        var auth = ctx.Request.Headers["Authorization"];
        if (auth.IsNullOrEmpty() || !auth.StartsWithIgnoreCase("Bearer ")) return false;

        var token = auth.Substring("Bearer ".Length).Trim();
        return StarAgentWebPanel.Current?.ValidateToken(token) ?? false;
    }
    #endregion

    #region 状态
    /// <summary>获取服务状态</summary>
    public Object Status()
    {
        if (!CheckAuth()) return new { code = 401, message = "Unauthorized" };

        var p = Process.GetCurrentProcess();
        var uptime = DateTime.Now - p.StartTime;
        var mi = MachineInfo.Current ?? MachineInfo.GetCurrent();
        mi.Refresh();
        mi.RefreshSpeed();

        // TCP 连接数
        var tcpConnections = 0;
        var tcpTimeWait = 0;
        var tcpCloseWait = 0;
        try
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            var connections = properties.GetActiveTcpConnections();
            tcpConnections = connections.Count(e => e.State == TcpState.Established);
            tcpTimeWait = connections.Count(e => e.State == TcpState.TimeWait);
            tcpCloseWait = connections.Count(e => e.State == TcpState.CloseWait);
        }
        catch { }

        // 物理内存总量
        long memoryTotalMB = mi.Memory > 0 ? (long)(mi.Memory / 1024 / 1024) : 0;
        if (memoryTotalMB <= 0 && Runtime.Windows)
        {
            try
            {
                GetPhysicallyInstalledSystemMemory(out var totalKB);
                memoryTotalMB = (long)(totalKB / 1024);
            }
            catch { }
        }

        return new
        {
            code = 0,
            data = new
            {
                serviceName = Service.ServiceName,
                displayName = Service.DisplayName,
                description = Service.Description,
                running = Service.Running,
                uptime = uptime.ToString(@"d\.hh\:mm\:ss"),
                uptimeSeconds = (Int64)uptime.TotalSeconds,
                processId = p.Id,
                memoryMB = p.WorkingSet64 / 1024 / 1024,
                memoryTotalMB,
                threadCount = p.Threads.Count,
                handleCount = p.HandleCount,
                startTime = p.StartTime.ToString("MM-dd HH:mm:ss"),
                hostMachine = Environment.MachineName,
                platform = mi.OSName ?? Environment.OSVersion.Platform.ToString(),
                osVersion = mi.OSVersion,
                cpuName = mi.Processor,
                cpuCount = Environment.ProcessorCount,
                cpuRate = mi.CpuRate > 0 ? mi.CpuRate.ToString("F1") : "",
                cpuRateValue = mi.CpuRate > 0 ? Math.Round(mi.CpuRate, 1) : 0d,
                availableMemory = mi.AvailableMemory > 0 ? $"{mi.AvailableMemory / 1024 / 1024 / 1024} GB" : "",
                board = mi.Board,
                machineGuid = mi.Guid,
                uplinkSpeed = mi.UplinkSpeed > 0 ? FormatSpeed(mi.UplinkSpeed) : "",
                downlinkSpeed = mi.DownlinkSpeed > 0 ? FormatSpeed(mi.DownlinkSpeed) : "",
                tcpConnections,
                tcpTimeWait,
                tcpCloseWait,
                hostUptime = TimeSpan.FromMilliseconds(Runtime.TickCount64).ToString(@"d\.hh\:mm\:ss"),
                port = StarAgentWebPanel.Current?.Port ?? 0
            }
        };
    }
    #endregion

    #region 服务控制
    /// <summary>服务控制（启停重启）</summary>
    public Object Control(String action)
    {
        if (!CheckAuth()) return new { code = 401, message = "Unauthorized" };
        if (action.IsNullOrEmpty()) return new { code = 400, message = "Missing action" };

        switch (action.ToLower())
        {
            case "stop":
                XTrace.WriteLine("Web面板触发服务停止");
                Service.Running = false;
                return new { code = 0, message = "服务正在停止" };
            case "start":
            case "restart":
                XTrace.WriteLine("Web面板触发服务重启");
                Service.Host.Restart(Service.ServiceName);
                return new { code = 0, message = "服务正在重启" };
            default:
                return new { code = 400, message = $"Unknown action: {action}" };
        }
    }

    /// <summary>获取应用服务列表</summary>
    public Object Services()
    {
        if (!CheckAuth()) return new { code = 401, message = "Unauthorized" };

        var mgr = GetServiceManager();
        var list = new List<Object>();
        if (mgr != null)
        {
            // 通过反射获取 _controllers 列表
            var controllersField = mgr.GetType().GetField("_controllers", BindingFlags.NonPublic | BindingFlags.Instance);
            var controllers = controllersField?.GetValue(mgr) as System.Collections.IList;

            foreach (var svc in mgr.Services)
            {
                ServiceController? ctrl = null;
                if (controllers != null)
                {
                    foreach (var c in controllers)
                    {
                        if (c is ServiceController sc && sc.Name.EqualIgnoreCase(svc.Name))
                        {
                            ctrl = sc;
                            break;
                        }
                    }
                }

                list.Add(new
                {
                    name = svc.Name,
                    fileName = svc.FileName,
                    arguments = svc.Arguments,
                    workingDirectory = svc.WorkingDirectory,
                    enable = svc.Enable,
                    mode = svc.Mode.ToString(),
                    running = ctrl?.Running ?? false,
                    processId = ctrl?.Process?.Id ?? 0,
                });
            }
        }

        return new { code = 0, data = new { services = list } };
    }

    /// <summary>控制应用服务（启动/停止/重启指定服务）</summary>
    public Object ServiceControl(String name, String action)
    {
        if (!CheckAuth()) return new { code = 401, message = "Unauthorized" };
        if (name.IsNullOrEmpty()) return new { code = 400, message = "Missing service name" };

        var mgr = GetServiceManager();
        if (mgr == null) return new { code = 500, message = "ServiceManager unavailable" };

        var svc = mgr.Services.FirstOrDefault(s => s.Name.EqualIgnoreCase(name));
        if (svc == null) return new { code = 404, message = $"Service '{name}' not found" };

        try
        {
            switch (action?.ToLower())
            {
                case "start":
                    mgr.Start(svc.Name);
                    return new { code = 0, message = $"服务 {name} 已启动" };
                case "stop":
                    mgr.Stop(svc.Name, "Web面板控制");
                    return new { code = 0, message = $"服务 {name} 已停止" };
                case "restart":
                    mgr.Stop(svc.Name, "Web面板重启");
                    Thread.Sleep(500);
                    mgr.Start(svc.Name);
                    return new { code = 0, message = $"服务 {name} 已重启" };
                default:
                    return new { code = 400, message = $"Unknown action: {action}" };
            }
        }
        catch (Exception ex)
        {
            return new { code = 500, message = ex.Message };
        }
    }
    #endregion

    #region 配置管理
    /// <summary>获取 Agent 配置面板 HTML（嵌入式资源）</summary>
    public Object Panel()
    {
        if (!CheckAuth()) return new { code = 401, message = "Unauthorized" };
        var html = _panelHtml.Value;
        return new { code = 0, data = new { html } };
    }

    /// <summary>获取配置（强类型 JSON）</summary>
    public Object GetConfig()
    {
        if (!CheckAuth()) return new { code = 401, message = "Unauthorized" };

        var set = GetAgentSetting();
        if (set == null) return new { code = 500, message = "无法获取配置实例" };

        var fields = GetFieldsMetadata(set);
        var services = (Array)set.GetType().GetProperty("Services")?.GetValue(set) ?? new Object[0];
        var svcList = new List<Dictionary<String, String>>();
        foreach (var s in services) svcList.Add(ServiceInfoToDict(s));

        return new { code = 0, data = new { fields, services = svcList } };
    }

    /// <summary>保存配置（接收 JSON，直接操作 StarAgentSetting.Current）</summary>
    public Object SaveConfig(String content)
    {
        if (!CheckAuth()) return new { code = 401, message = "Unauthorized" };
        if (content.IsNullOrEmpty()) return new { code = 400, message = "配置内容不能为空" };

        try
        {
            var json = JsonParser.Decode(content);
            var set = GetAgentSetting();
            if (set == null) return new { code = 500, message = "无法获取配置实例" };

            var setType = set.GetType();

            // 更新普通字段
            var fields = json["fields"] as IList<Object>;
            if (fields != null)
            {
                foreach (var fObj in fields)
                {
                    var f = fObj as IDictionary<String, Object>;
                    var name = f["name"]?.ToString();
                    var value = f["value"]?.ToString();
                    if (name.IsNullOrEmpty() || name == "Services") continue;

                    var prop = setType.GetProperty(name);
                    if (prop == null || !prop.CanWrite) continue;

                    var converted = ConvertValue(value, prop.PropertyType);
                    if (converted != null) prop.SetValue(set, converted);
                }
            }

            // 更新服务列表
            var services = json["services"] as IList<Object>;
            if (services != null)
            {
                var list = new List<ServiceInfo>();
                foreach (var svcObj in services)
                {
                    var svc = svcObj as IDictionary<String, Object>;
                    var info = new ServiceInfo();
                    foreach (var kv in svc) SetServiceInfoField(info, kv.Key, kv.Value?.ToString());
                    list.Add(info);
                }
                var servicesProp = setType.GetProperty("Services");
                servicesProp?.SetValue(set, list.ToArray());
            }

            // 调用 Save()
            var saveMethod = setType.GetMethod("Save", Type.EmptyTypes);
            saveMethod?.Invoke(set, null);

            return new { code = 0, message = "配置已保存，文件变更将自动重载" };
        }
        catch (Exception ex)
        {
            return new { code = 500, message = "保存失败：" + ex.Message };
        }
    }
    #endregion

    #region 日志
    /// <summary>获取日志内容</summary>
    public Object Logs(Int32 count, String file, String level)
    {
        if (!CheckAuth()) return new { code = 401, message = "Unauthorized" };

        if (count <= 0) count = 200;
        count = Math.Min(count, 1000);

        var lines = ReadLogLines(count, file, level);
        return new { code = 0, data = new { fileName = file ?? "latest", count = lines.Count, lines } };
    }

    /// <summary>获取日志文件列表</summary>
    public Object LogFiles()
    {
        if (!CheckAuth()) return new { code = 401, message = "Unauthorized" };

        var list = new List<Object>();
        try
        {
            var logDir = NewLife.Setting.Current.LogPath.GetFullPath();
            if (Directory.Exists(logDir))
            {
                foreach (var filePath in Directory.GetFiles(logDir, "*.log").OrderByDescending(f => f))
                {
                    var fi = new FileInfo(filePath);
                    list.Add(new
                    {
                        name = fi.Name,
                        size = fi.Length,
                        sizeDisplay = fi.Length.ToGMK(),
                        lastModified = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }
            }
        }
        catch (Exception ex) { XTrace.WriteException(ex); }

        return new { code = 0, data = new { files = list } };
    }

    private static List<String> ReadLogLines(Int32 count, String? fileName, String? level)
    {
        var list = new List<String>();
        try
        {
            var logDir = NewLife.Setting.Current.LogPath.GetFullPath();
            if (logDir == null) return list;

            String? logFile;
            if (!fileName.IsNullOrEmpty())
            {
                var safeName = Path.GetFileName(fileName);
                logFile = Path.Combine(logDir, safeName);
                if (!File.Exists(logFile)) logFile = null;
            }
            else
            {
                logFile = Directory.GetFiles(logDir, "*.log").OrderByDescending(f => f).FirstOrDefault();
            }
            if (logFile == null) return list;

            var allLines = File.ReadAllLines(logFile);
            var start = Math.Max(0, allLines.Length - count);
            for (var i = start; i < allLines.Length; i++)
            {
                var line = allLines[i];
                if (!level.IsNullOrEmpty() && !line.Contains(level, StringComparison.OrdinalIgnoreCase))
                    continue;
                list.Add(line);
            }
        }
        catch (Exception ex) { XTrace.WriteException(ex); }

        return list;
    }
    #endregion

    #region 辅助方法
    private ServiceManager? GetServiceManager()
    {
        // 通过反射获取 MyService._Manager 字段
        var field = Service.GetType().GetField("_Manager", BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(Service) as ServiceManager;
    }

    private Object? GetAgentSetting()
    {
        var prop = Service.GetType().GetProperty("AgentSetting");
        return prop?.GetValue(Service);
    }

    private List<Object> GetFieldsMetadata(Object set)
    {
        var list = new List<Object>();
        var type = set.GetType();
        var editable = new HashSet<String> { "Debug", "Delay", "SyncTime", "StartupHook" };
        var systemFields = new HashSet<String> { "UserName", "Dpi", "Resolution" };

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.Name == "Services") continue;
            if (prop.GetIndexParameters().Length > 0) continue;

            var value = prop.GetValue(set);
            var desc = prop.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "";
            var category = editable.Contains(prop.Name) ? "editable" :
                          systemFields.Contains(prop.Name) ? "system" : "readonly";

            list.Add(new
            {
                name = prop.Name,
                value = value?.ToString() ?? "",
                comment = desc,
                category,
                type = prop.PropertyType.Name.ToLower(),
            });
        }
        return list;
    }

    private static Dictionary<String, String> ServiceInfoToDict(Object s)
    {
        var dict = new Dictionary<String, String>();
        if (s == null) return dict;
        var type = s.GetType();
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.GetIndexParameters().Length > 0) continue;
            dict[prop.Name] = prop.GetValue(s)?.ToString() ?? "";
        }
        return dict;
    }

    private static void SetServiceInfoField(ServiceInfo info, String fieldName, String? value)
    {
        var prop = info.GetType().GetProperty(fieldName);
        if (prop == null || !prop.CanWrite) return;
        var converted = ConvertValue(value, prop.PropertyType);
        if (converted != null) prop.SetValue(info, converted);
    }

    private static Object? ConvertValue(String? value, Type targetType)
    {
        try
        {
            if (targetType == typeof(String)) return value ?? "";
            if (targetType == typeof(Boolean)) return value.EqualIgnoreCase("true");
            if (targetType == typeof(Int32)) return Int32.TryParse(value, out var i) ? i : 0;
            if (targetType == typeof(Int64)) return Int64.TryParse(value, out var l) ? l : 0L;
            if (targetType == typeof(Double)) return Double.TryParse(value, out var d) ? d : 0.0;
            if (targetType.IsEnum) return Enum.Parse(targetType, value ?? "", true);
            return value;
        }
        catch { return null; }
    }

    private static String FormatSpeed(UInt64 bps) => bps switch
    {
        < 1000UL => $"{bps} bps",
        < 1000_000UL => $"{bps / 1000.0:F1} Kbps",
        < 1000_000_000UL => $"{bps / 1000_000.0:F1} Mbps",
        _ => $"{bps / 1000_000_000.0:F2} Gbps"
    };

    private static readonly Lazy<String> _panelHtml = new(() =>
    {
        var asm = typeof(StarAgentApiController).Assembly;
        var resourceName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("staragent_panel.html", StringComparison.OrdinalIgnoreCase));
        if (resourceName.IsNullOrEmpty()) return "<p>面板资源未找到</p>";

        using var stream = asm.GetManifestResourceStream(resourceName);
        if (stream == null) return "<p>面板资源流未找到</p>";
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    });
    #endregion
}
#endif
