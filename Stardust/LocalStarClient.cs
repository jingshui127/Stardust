﻿using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NewLife;
using NewLife.Http;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Remoting;
using Stardust.Models;

namespace Stardust;

/// <summary>本地星尘客户端。连接本机星尘代理StarAgent</summary>
public class LocalStarClient
{
    #region 属性
    /// <summary>本机代理信息</summary>
    public AgentInfo Info { get; private set; }

    /// <summary>本地服务端地址</summary>
    public String Server { get; set; }

    private AgentInfo _local;
    private ApiClient _client;
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public LocalStarClient()
    {
        //_local = AgentInfo.GetLocal();
        //_local.Server = StarSetting.Current.Server;
    }
    #endregion

    #region 方法
    private void Init()
    {
        if (_client != null) return;

        _client = new ApiClient("udp://127.0.0.1:5500")
        {
            Timeout = 3_000,
            Log = Log,
        };

        var set = StarSetting.Current;
        if (set.Debug) _client.EncoderLog = Log;

        _local = AgentInfo.GetLocal(false);
        _local.Server = !Server.IsNullOrEmpty() ? Server : StarSetting.Current.Server;
    }

    /// <summary>获取信息</summary>
    /// <returns></returns>
    public AgentInfo GetInfo()
    {
        var task = Task.Run(GetInfoAsync);
        return task.Wait(500) ? task.Result : null;
    }

    /// <summary>获取信息</summary>
    /// <returns></returns>
    public async Task<AgentInfo> GetInfoAsync()
    {
        Init();

        try
        {
            return Info = await _client.InvokeAsync<AgentInfo>("Info", _local);
        }
        catch (TimeoutException)
        {
            return null;
        }
        catch
        {
            throw;
        }
    }
    #endregion

    #region 进程控制
    /// <summary>自杀并重启</summary>
    /// <returns></returns>
    public Boolean KillAndRestartMySelf()
    {
        Init();

        var p = Process.GetCurrentProcess();
        var fileName = p.MainModule.FileName;
        var args = Environment.CommandLine.TrimStart(Path.ChangeExtension(fileName, ".dll")).Trim();

        // 发起命令
        var rs = _client.Invoke<String>("KillAndStart", new
        {
            processId = p.Id,
            delay = 3,
            fileName,
            arguments = args,
            workingDirectory = Environment.CurrentDirectory,
        });

        // 本进程退出
        //p.Kill();

        return !rs.IsNullOrEmpty();
    }
    #endregion

    #region 安装星尘代理
    /// <summary>探测并安装星尘代理</summary>
    /// <param name="url">zip包下载源</param>
    /// <param name="version">版本号</param>
    /// <param name="target">目标目录</param>
    public Boolean ProbeAndInstall(String url = null, String version = null, String target = null)
    {
        //if (url.IsNullOrEmpty()) throw new ArgumentNullException(nameof(url));
        if (url.IsNullOrEmpty())
        {
            var set = NewLife.Setting.Current;
            url = set.PluginServer.EnsureEnd("/");
            url += "star/";
            if (Environment.Version.Major >= 6)
                url += "staragent60.zip";
            else if (Environment.Version.Major >= 5)
                url += "staragent50.zip";
            else if (Environment.Version.Major >= 4)
                url += "staragent45.zip";
            else
                url += "staragent31.zip";
        }

        // 尝试连接，获取版本
        try
        {
            var info = GetInfo();
            if (info != null)
            {
                // 比目标版本高，不需要安装
                if (String.Compare(info.Version, version) >= 0) return true;

                if (!info.FileName.IsNullOrEmpty()) info.FileName = info.FileName.TrimEnd(" (deleted)");
                if (target.IsNullOrEmpty()) target = Path.GetDirectoryName(info.FileName);

                WriteLog("StarAgent在用版本 v{0}，低于目标版本 v{1}", info.Version, version);
            }
        }
        catch (Exception ex)
        {
            WriteLog("没有探测到StarAgent，{0}", ex.GetTrue().Message);
        }

        if (target.IsNullOrEmpty())
        {
            // 在进程中查找
            var p = Process.GetProcesses().FirstOrDefault(e => e.ProcessName == "StarAgent");
            if (p != null)
            {
                try
                {
                    target = Path.GetDirectoryName(p.MainModule.FileName);
                }
                catch
                {
                    target = Path.GetDirectoryName(p.MainWindowTitle);
                }

                WriteLog("发现进程StarAgent，ProcessId={0}，target={1}", p.Id, target);
            }
        }

        // 准备安装，甭管是否能够成功重启，先覆盖了文件再说
        {
            if (target.IsNullOrEmpty()) target = "..\\staragent";
            target = target.GetFullPath();
            target.EnsureDirectory(false);

            WriteLog("目标：{0}", target);

            var ug = new Stardust.Web.Upgrade
            {
                SourceFile = Path.GetFileName(url).GetFullPath(),
                DestinationPath = target,

                Log = XTrace.Log,
            };

            WriteLog("下载：{0}", url);

            var client = new HttpClient();
            client.DownloadFileAsync(url, ug.SourceFile).Wait();

            ug.Extract();
            ug.Update();

            File.Delete(ug.SourceFile);
        }

        {
            // 在进程中查找
            var info = Info;
            var inService = info?.Arguments == "-s";
            var p = info != null && info.ProcessId > 0 ?
                Process.GetProcessById(info.ProcessId) :
                Process.GetProcesses().FirstOrDefault(e => e.ProcessName == "StarAgent");

            // 重启目标
            if (p != null && !inService)
            {
                try
                {
                    p.Kill();
                }
                catch (Win32Exception) { }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
            }

            var fileName = info?.FileName;
            if (!fileName.IsNullOrEmpty() && Path.GetFullPath(fileName).EqualIgnoreCase("dotnet.exe")) fileName = info.Arguments;

            var rs = false;
            if (Runtime.Windows)
                rs = RunAgentOnWindows(fileName, target, inService);
            else if (Runtime.Linux)
                rs = RunAgentOnLinux(fileName, target, inService);
            if (!rs)
                rs = RunAgentOnDotnet(fileName, target, inService);
        }

        return true;
    }

    private Boolean RunAgentOnWindows(String fileName, String target, Boolean inService)
    {
        if (!fileName.IsNullOrEmpty() && Path.GetExtension(fileName) == ".dll") return false;
        if (fileName.IsNullOrEmpty()) fileName = target.CombinePath("StarAgent.exe").GetFullPath();
        if (!File.Exists(fileName)) return false;

        WriteLog("RunAgentOnWindows fileName={0}, inService={1}", fileName, inService);

        if (inService)
        {
            Process.Start(fileName, "-stop");
            Process.Start(fileName, "-start");

            WriteLog("启动服务成功");
        }
        else
        {
            var si = new ProcessStartInfo(fileName, "-run")
            {
                WorkingDirectory = Path.GetDirectoryName(fileName),
                UseShellExecute = true
            };
            var p = Process.Start(si);

            WriteLog("启动进程成功 pid={0}", p.Id);
        }

        return true;
    }

    private Boolean RunAgentOnLinux(String fileName, String target, Boolean inService)
    {
        if (!fileName.IsNullOrEmpty() && Path.GetExtension(fileName) == ".dll") return false;
        if (fileName.IsNullOrEmpty()) fileName = target.CombinePath("StarAgent").GetFullPath();
        if (!File.Exists(fileName)) return false;

        WriteLog("RunAgentOnLinux fileName={0}, inService={1}", fileName, inService);

        // 在Linux中设置执行权限
        Process.Start("chmod", $"+x {fileName}");

        if (inService)
        {
            Process.Start(fileName, "-stop");
            Process.Start(fileName, "-start");

            WriteLog("启动服务成功");
        }
        else
        {
            var si = new ProcessStartInfo(fileName, "-run")
            {
                WorkingDirectory = Path.GetDirectoryName(fileName),
                UseShellExecute = true
            };
            var p = Process.Start(si);

            WriteLog("启动进程成功 pid={0}", p.Id);
        }

        return true;
    }

    private Boolean RunAgentOnDotnet(String fileName, String target, Boolean inService)
    {
        if (fileName.IsNullOrEmpty()) fileName = target.CombinePath("StarAgent.dll").GetFullPath();
        if (!File.Exists(fileName)) return false;

        WriteLog("RunAgentOnDotnet fileName={0}, inService={1}", fileName, inService);

        if (inService)
        {
            Process.Start("dotnet", $"{fileName} -stop");
            Process.Start("dotnet", $"{fileName} -start");

            WriteLog("启动服务成功");
        }
        else
        {
            var si = new ProcessStartInfo("dotnet", $"{fileName} -run")
            {
                WorkingDirectory = Path.GetDirectoryName(fileName),
                UseShellExecute = true
            };
            var p = Process.Start(si);

            WriteLog("启动进程成功 pid={0}", p.Id);
        }

        return true;
    }

    /// <summary>探测并安装星尘代理</summary>
    /// <param name="url">zip包下载源</param>
    /// <param name="version">版本号</param>
    /// <param name="target">目标目录</param>
    public static Task ProbeAsync(String url = null, String version = null, String target = null)
    {
        return Task.Run(() =>
        {
            var client = new LocalStarClient();
            client.ProbeAndInstall(url, version, target);
        });
    }
    #endregion

    #region 安装卸载应用服务
    ///// <summary>安装应用服务（星尘代理守护）</summary>
    ///// <param name="service"></param>
    ///// <returns></returns>
    //public async Task<ProcessInfo> Install(ServiceInfo service)
    //{
    //    Init();

    //    return await _client.InvokeAsync<ProcessInfo>("Install", service);
    //}

    ///// <summary>安装应用服务（星尘代理守护）</summary>
    ///// <param name="name">服务名，唯一标识</param>
    ///// <param name="fileName">文件</param>
    ///// <param name="arguments">参数</param>
    ///// <param name="workingDirectory">工作目录</param>
    ///// <returns></returns>
    //public async Task<ProcessInfo> Install(String name, String fileName, String arguments = null, String workingDirectory = null)
    //{
    //    Init();

    //    return await _client.InvokeAsync<ProcessInfo>("Install", new ServiceInfo
    //    {
    //        Name = name,
    //        FileName = fileName,
    //        Arguments = arguments,
    //        WorkingDirectory = workingDirectory,
    //        Enable = true,
    //        //ReloadOnChange = true,
    //    });
    //}

    ///// <summary>卸载应用服务</summary>
    ///// <param name="serviceName"></param>
    ///// <returns></returns>
    //public async Task<Boolean> Uninstall(String serviceName)
    //{
    //    Init();

    //    return await _client.InvokeAsync<Boolean>("Uninstall", serviceName);
    //}
    #endregion

    #region 搜索
    /// <summary>在局域网中广播扫描所有StarAgent</summary>
    /// <param name="local">本地信息，用于告知对方我是谁</param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    public static IEnumerable<AgentInfo> Scan(AgentInfo local = null, Int32 timeout = 15_000)
    {
        var encoder = new JsonEncoder { Log = XTrace.Log };
        // 构造请求消息
        //var ms = new MemoryStream();
        //var writer = new BinaryWriter(ms);
        //writer.Write("Info");
        //writer.Write(0);

        //var msg = new DefaultMessage
        //{
        //    Payload = ms.ToArray()
        //};
        //var buf = msg.ToPacket().ToArray();

        var buf = encoder.CreateRequest("Info", null).ToPacket().ToArray();

        // 广播消息
        var udp = new UdpClient();
        udp.Send(buf, buf.Length, new IPEndPoint(IPAddress.Broadcast, 5500));

        var end = DateTime.Now.AddSeconds(timeout);
        while (DateTime.Now < end)
        {
            var rs = new DefaultMessage();
            IPEndPoint ep = null;
            buf = udp.Receive(ref ep);
            if (buf != null && rs.Read(buf) && encoder.Decode(rs, out var action, out _, out var data))
            {
                //ms = rs.Payload.GetStream();
                //var reader=new BinaryReader(ms);
                //var name=reader.ReadString();
                //var code = reader.ReadInt32();
                //var data=reader

                var js = encoder.DecodeResult(action, data, rs);
                var info = (AgentInfo)encoder.Convert(js, typeof(AgentInfo));

                yield return info;
            }
        }
    }
    #endregion

    #region 日志
    /// <summary>日志</summary>
    public ILog Log { get; set; }

    /// <summary>写日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteLog(String format, params Object[] args) => Log?.Info(format, args);
    #endregion
}