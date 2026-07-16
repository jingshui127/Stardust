#if !NET40
using NewLife;
using NewLife.Agent;
using NewLife.Http;
using NewLife.Log;

namespace StarAgent.WebPanel;

/// <summary>StarAgent Web 管理面板（完全自建，不依赖 NewLife.Agent.WebPanel）</summary>
/// <remarks>
/// 基于 NewLife.Http.HttpServer 自建完整 Web 管理面板，包含状态监控、服务控制、
/// 配置管理、日志查看、关于页面等功能。NewLife.Agent 仅作为 NuGet 包引用，
/// 使用其 ServiceBase/ServiceManager/MachineInfo 等核心 API，不使用其 WebPanel。
/// </remarks>
public class StarAgentWebPanel
{
    #region 属性
    /// <summary>当前面板实例（供控制器访问）</summary>
    public static StarAgentWebPanel? Current { get; private set; }

    /// <summary>Http服务器</summary>
    public HttpServer Server { get; }

    /// <summary>所属服务</summary>
    public ServiceBase Service { get; }

    /// <summary>用户名</summary>
    public String UserName { get; set; }

    /// <summary>密码</summary>
    public String Password { get; set; }

    /// <summary>是否运行中</summary>
    public Boolean Active => Server.Active;

    /// <summary>实际监听端口</summary>
    public Int32 Port => Server.Port;
    #endregion

    #region 构造
    /// <summary>实例化Web管理面板</summary>
    /// <param name="service">所属服务</param>
    public StarAgentWebPanel(ServiceBase service)
    {
        Service = service ?? throw new ArgumentNullException(nameof(service));
        Current = this;

        var set = NewLife.Agent.Setting.Current;
        var port = set.WebPort > 0 ? set.WebPort : 5581;

        Server = new HttpServer
        {
            Port = port,
            ServerName = $"StarAgent/{Service.ServiceName}",
            Log = XTrace.Log
        };

        UserName = set.WebUserName;
        Password = set.WebPassword;
    }
    #endregion

    #region Token管理
    private class TokenInfo
    {
        public String User { get; set; } = "";
        public DateTime Expire { get; set; }
    }

    private static readonly Dictionary<String, TokenInfo> _tokens = [];
    private static readonly Object _lock = new();

    /// <summary>签发Token</summary>
    public String? IssueToken(String user, String password)
    {
        if (user.IsNullOrEmpty() || password.IsNullOrEmpty()) return null;
        if (UserName.IsNullOrEmpty() || Password.IsNullOrEmpty()) return null;
        if (!user.EqualIgnoreCase(UserName)) return null;
        if (password != Password) return null;

        var token = Guid.NewGuid().ToString("N");
        var info = new TokenInfo { User = user, Expire = DateTime.Now.AddHours(24) };

        lock (_lock)
        {
            // 清理过期Token
            var expired = _tokens.Where(e => e.Value.Expire < DateTime.Now).Select(e => e.Key).ToList();
            foreach (var key in expired) _tokens.Remove(key);
            _tokens[token] = info;
        }

        return token;
    }

    /// <summary>验证Token</summary>
    public Boolean ValidateToken(String? token)
    {
        if (token.IsNullOrEmpty()) return false;

        lock (_lock)
        {
            if (!_tokens.TryGetValue(token!, out var info)) return false;
            if (info.Expire < DateTime.Now)
            {
                _tokens.Remove(token!);
                return false;
            }
            return true;
        }
    }
    #endregion

    #region 启动停止
    public void Start()
    {
        if (Server.Active) return;

        // 注册路由
        Server.MapController<StarAgentApiController>("/api");
        Server.MapEmbedded<StarAgentWebPanel>("/", "StarAgent.wwwroot");

        Server.Start();
        XTrace.WriteLine("StarAgent Web 面板已启动，端口：{0}", Server.Port);
    }

    public void Stop(String reason)
    {
        if (!Server.Active) return;
        Server.Stop(reason ?? "ServiceStop");
        XTrace.WriteLine("StarAgent Web 面板已停止：{0}", reason);
    }
    #endregion
}
#endif
