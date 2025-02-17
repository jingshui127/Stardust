﻿using System.Collections;
using System.Diagnostics;
using System.Reflection;
using NewLife;
using NewLife.Configuration;
using NewLife.Http;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Security;
using Stardust.Configs;
using Stardust.Models;
using Stardust.Monitors;
using Stardust.Registry;
using Stardust.Services;

namespace Stardust;

/// <summary>星尘工厂</summary>
/// <remarks>
/// 星尘代理 https://newlifex.com/blood/staragent_install
/// 监控中心 https://newlifex.com/blood/stardust_monitor
/// 配置中心 https://newlifex.com/blood/stardust_configcenter
/// </remarks>
public class StarFactory : DisposeBase
{
    #region 属性
    /// <summary>服务器地址</summary>
    public String Server { get; set; }

    /// <summary>应用</summary>
    public String AppId { get; set; }

    /// <summary>应用名</summary>
    public String AppName { get; set; }

    /// <summary>应用密钥</summary>
    public String Secret { get; set; }

    /// <summary>实例。应用可能多实例部署，ip@proccessid</summary>
    public String ClientId { get; set; }

    ///// <summary>服务名</summary>
    //public String ServiceName { get; set; }

    /// <summary>客户端</summary>
    public IApiClient Client => _client;

    /// <summary>应用客户端</summary>
    public AppClient App => _client;

    /// <summary>配置信息。从配置中心返回的信息头</summary>
    public ConfigInfo ConfigInfo => (_config as StarHttpConfigProvider)?.ConfigInfo;

    /// <summary>本地星尘代理</summary>
    public LocalStarClient Local { get; private set; }

    private AppClient _client;
    private TokenHttpFilter _tokenFilter;
    //private AppClient _appClient;
    #endregion

    #region 构造
    /// <summary>
    /// 实例化星尘工厂，先后读取appsettings.json、本地StarAgent、star.config
    /// </summary>
    public StarFactory() => Init();

    /// <summary>实例化星尘工厂，指定地址、应用和密钥，创建工厂</summary>
    /// <param name="server">服务端地址。为空时先后读取appsettings.json、本地StarAgent、star.config，初始值为空，不连接服务端</param>
    /// <param name="appId">应用标识。为空时读取star.config，初始值为入口程序集名称</param>
    /// <param name="secret">应用密钥。为空时读取star.config，初始值为空</param>
    /// <returns></returns>
    public StarFactory(String server, String appId, String secret)
    {
        Server = server;
        AppId = appId;
        Secret = secret;

        Init();
    }

    /// <summary>销毁</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        _tracer.TryDispose();
        _config.TryDispose();
        //_appClient.TryDispose();
    }

    private void Init()
    {
        XTrace.WriteLine("正在初始化星尘……");

        Local = new LocalStarClient();

        // 从环境变量读取星尘地址、应用Id、密钥，方便容器化部署
        if (Server.IsNullOrEmpty()) Server = Environment.GetEnvironmentVariable("StarServer");
        if (AppId.IsNullOrEmpty()) AppId = Environment.GetEnvironmentVariable("AppId");
        if (Secret.IsNullOrEmpty()) Secret = Environment.GetEnvironmentVariable("Secret");

        // 不区分大小写识别环境变量
        foreach (DictionaryEntry item in Environment.GetEnvironmentVariables())
        {
            var key = item.Key + "";
            if (Server.IsNullOrEmpty() && key.EqualIgnoreCase("StarServer"))
                Server = item.Value + "";
            else if (AppId.IsNullOrEmpty() && key.EqualIgnoreCase("AppId"))
                AppId = item.Value + "";
            else if (Secret.IsNullOrEmpty() && key.EqualIgnoreCase("Secret"))
                Secret = item.Value + "";
        }

        // 读取本地appsetting
        if (Server.IsNullOrEmpty() && File.Exists("appsettings.Development.json".GetFullPath()))
        {
            using var json = new JsonConfigProvider { FileName = "appsettings.Development.json" };
            json.LoadAll();

            Server = json["StarServer"];
        }
        if (Server.IsNullOrEmpty() && File.Exists("appsettings.json".GetFullPath()))
        {
            using var json = new JsonConfigProvider { FileName = "appsettings.json" };
            json.LoadAll();

            Server = json["StarServer"];
        }

        if (!Server.IsNullOrEmpty() && Local.Server.IsNullOrEmpty()) Local.Server = Server;

        var flag = false;
        var set = StarSetting.Current;

        if (AppId != "StarAgent")
        {
            // 借助本地StarAgent获取服务器地址
            try
            {
                //XTrace.WriteLine("正在探测本机星尘代理……");
                var inf = Local.GetInfo();
                var server = inf?.Server;
                if (!server.IsNullOrEmpty())
                {
                    if (Server.IsNullOrEmpty()) Server = server;
                    XTrace.WriteLine("星尘探测：{0}", server);

                    if (set.Server.IsNullOrEmpty())
                    {
                        set.Server = server;
                        flag = true;
                    }
                }
                else
                    XTrace.WriteLine("星尘探测：StarAgent Not Found");
            }
            catch (Exception ex)
            {
                XTrace.Log.Error("星尘探测失败！{0}", ex.Message);
            }
        }

        // 如果探测不到本地应用，则使用配置
        if (Server.IsNullOrEmpty()) Server = set.Server;
        if (AppId.IsNullOrEmpty()) AppId = set.AppKey;
        if (Secret.IsNullOrEmpty()) Secret = set.Secret;

        if (flag) set.Save();

        // 生成ClientId，用于唯一标识当前实例，默认IP@pid
        try
        {
            var executing = AssemblyX.Create(Assembly.GetExecutingAssembly());
            var asm = AssemblyX.Entry ?? executing;
            if (asm != null)
            {
                if (AppId.IsNullOrEmpty()) AppId = asm.Name;
                if (AppName.IsNullOrEmpty()) AppName = asm.Title;
            }

            ClientId = $"{NetHelper.MyIP()}@{Process.GetCurrentProcess().Id}";
        }
        catch
        {
            ClientId = Rand.NextString(8);
        }

        XTrace.WriteLine("星尘分布式服务 Server={0} AppId={1} ClientId={2}", Server, AppId, ClientId);

        Valid();

        var ioc = ObjectContainer.Current;
        ioc.AddSingleton(this);
        ioc.AddSingleton(p => Tracer);
        ioc.AddSingleton(p => Config);
        ioc.AddSingleton(p => Service);
    }

    private Boolean Valid()
    {
        //if (Server.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Server));
        //if (AppId.IsNullOrEmpty()) throw new ArgumentNullException(nameof(AppId));

        if (Server.IsNullOrEmpty() || AppId.IsNullOrEmpty()) return false;

        if (_client == null)
        {
            if (!AppId.IsNullOrEmpty()) _tokenFilter = new TokenHttpFilter
            {
                UserName = AppId,
                Password = Secret,
                ClientId = ClientId,
            };

            var client = new AppClient(Server)
            {
                AppId = AppId,
                AppName = AppName,
                ClientId = ClientId,
                NodeCode = Local?.Info?.Code,
                Filter = _tokenFilter,
                UseWebSocket = true,

                Log = Log,
            };

            //var set = StarSetting.Current;
            //if (set.Debug) client.Log = XTrace.Log;
            client.WriteInfoEvent("应用启动", $"pid={Process.GetCurrentProcess().Id}");

            _client = client;

            InitTracer();

            client.Tracer = _tracer;
            client.Start();

            // 注册StarServer环境变量，子进程共享
            Environment.SetEnvironmentVariable("StarServer", Server);
        }

        return true;
    }
    #endregion

    #region 监控中心
    private StarTracer _tracer;
    /// <summary>监控中心</summary>
    public ITracer Tracer
    {
        get
        {
            if (_tracer == null)
            {
                if (!Valid()) return null;

                InitTracer();
            }

            return _tracer;
        }
    }

    private void InitTracer()
    {
        XTrace.WriteLine("初始化星尘监控中心，采样并定期上报应用性能埋点数据，包括Api接口、Http请求、数据库操作、Redis操作等。可用于监控系统健康状态，分析分布式系统的性能瓶颈。");

        var tracer = new StarTracer(Server)
        {
            AppId = AppId,
            AppName = AppName,
            //Secret = Secret,
            ClientId = ClientId,
            Client = _client,

            Log = Log
        };

        tracer.AttachGlobal();
        _tracer = tracer;
    }
    #endregion

    #region 配置中心
    private HttpConfigProvider _config;
    /// <summary>配置中心。务必在数据库操作和生成雪花Id之前使用激活</summary>
    /// <remarks>
    /// 文档 https://newlifex.com/blood/stardust_configcenter
    /// </remarks>
    public IConfigProvider Config
    {
        get
        {
            if (_config == null)
            {
                if (!Valid()) return null;

                XTrace.WriteLine("初始化星尘配置中心，提供集中配置管理能力，自动从配置中心加载配置数据，包括XCode数据库连接。配置中心同时支持分配应用实例的唯一WorkerId，确保Snowflake算法能够生成绝对唯一的雪花Id");

                var config = new StarHttpConfigProvider
                {
                    Server = Server,
                    AppId = AppId,
                    //Secret = Secret,
                    ClientId = ClientId,
                    Client = _client,
                };
                //if (!ClientId.IsNullOrEmpty()) config.ClientId = ClientId;
                config.Attach(_client);

                //!! 不需要默认加载，直到首次使用配置数据时才加载。因为有可能应用并不使用配置中心，仅仅是获取这个对象。避免网络不通时的报错日志
                //config.LoadAll();

                _config = config;
            }

            return _config;
        }
    }

    private IConfigProvider _configProvider;
    /// <summary>设置本地配置提供者，该提供者将跟星尘配置结合到一起，形成复合配置提供者</summary>
    /// <param name="configProvider"></param>
    public void SetLocalConfig(IConfigProvider configProvider)
    {
        if (configProvider == null) return;

        var cfg = Config;
        if (cfg != null)
            _configProvider = new CompositeConfigProvider(configProvider, cfg);
        else
            _configProvider = configProvider;
    }

    /// <summary>获取复合配置提供者</summary>
    /// <returns></returns>
    public IConfigProvider GetConfig() => _configProvider ?? Config;
    #endregion

    #region 注册中心
    private Boolean _initService;
    /// <summary>注册中心，服务注册与发现</summary>
    public IRegistry Service
    {
        get
        {
            if (!_initService)
            {
                if (!Valid()) return null;

                _initService = true;
                //_appClient = _client as AppClient;

                XTrace.WriteLine("初始化星尘注册中心，提供服务注册与发布能力");
            }

            return _client;
        }
    }

    /// <summary>为指定服务创建客户端，从星尘注册中心获取服务地址。单例，应避免频繁创建客户端</summary>
    /// <param name="serviceName"></param>
    /// <param name="tag"></param>
    /// <returns></returns>
    public IApiClient CreateForService(String serviceName, String tag = null) => Task.Run(() => CreateForServiceAsync(serviceName, tag)).Result;

    /// <summary>为指定服务创建客户端，从星尘注册中心获取服务地址。单例，应避免频繁创建客户端</summary>
    /// <param name="serviceName"></param>
    /// <param name="tag"></param>
    /// <returns></returns>
    public Task<IApiClient> CreateForServiceAsync(String serviceName, String tag = null) => Service.CreateForServiceAsync(serviceName, tag);

    /// <summary>发布服务</summary>
    /// <param name="serviceName">服务名</param>
    /// <param name="address">服务地址</param>
    /// <param name="tag">特性标签</param>
    /// <param name="health">健康监测接口地址</param>
    /// <returns></returns>
    public Task<PublishServiceInfo> RegisterAsync(String serviceName, String address, String tag = null, String health = null) => Service.RegisterAsync(serviceName, address, tag, health);

    /// <summary>消费得到服务地址信息</summary>
    /// <param name="serviceName">服务名</param>
    /// <param name="minVersion">最小版本</param>
    /// <param name="tag">特性标签。只要包含该特性的服务提供者</param>
    /// <returns></returns>
    public Task<String[]> ResolveAddressAsync(String serviceName, String minVersion = null, String tag = null) => Service.ResolveAddressAsync(serviceName, minVersion, tag);
    #endregion

    #region 其它
    /// <summary>发送节点命令</summary>
    /// <param name="nodeCode"></param>
    /// <param name="command"></param>
    /// <param name="argument"></param>
    /// <param name="expire"></param>
    /// <returns></returns>
    public async Task<Int32> SendNodeCommand(String nodeCode, String command, String argument = null, Int32 expire = 3600)
    {
        if (!Valid()) return -1;

        return await _client.PostAsync<Int32>("Node/SendCommand", new { Code = nodeCode, command, argument, expire });
    }

    /// <summary>发送应用命令</summary>
    /// <param name="appId"></param>
    /// <param name="command"></param>
    /// <param name="argument"></param>
    /// <param name="expire"></param>
    /// <returns></returns>
    public async Task<Int32> SendAppCommand(String appId, String command, String argument = null, Int32 expire = 3600)
    {
        if (!Valid()) return -1;

        return await _client.PostAsync<Int32>("App/SendCommand", new { Code = appId, command, argument, expire });
    }
    #endregion

    #region 日志
    /// <summary>日志。默认 XTrace.Log</summary>
    public ILog Log { get; set; } = XTrace.Log;
    #endregion
}