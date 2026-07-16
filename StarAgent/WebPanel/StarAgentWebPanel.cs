#if !NET40
using NewLife.Agent.WebPanel;
using NewLife.Http;
using NewLife.Log;
using NewLife.Agent;
using NewLife.Agent.Models;

namespace StarAgent.WebPanel;

/// <summary>StarAgent Web 管理面板（继承 AgentWebPanel，复用完整功能 + 强类型配置管理）</summary>
/// <remarks>
/// 继承 NewLife.Agent.WebPanel.AgentWebPanel，自动拥有 Status/Control/Logs/Health/WatchDog 等完整功能。
/// 重写 RegisterRoutes() 注册 StarAgentApiController（继承 ApiController + 配置管理方法）。
/// 重写 GetExtensions() 添加 Agent 配置管理面板。
/// </remarks>
public class StarAgentWebPanel : AgentWebPanel
{
    #region 构造
    public StarAgentWebPanel(ServiceBase service) : base(service)
    {
    }
    #endregion

    #region 路由注册
    protected override void RegisterRoutes()
    {
        // 注册 StarAgentApiController（继承 ApiController，拥有完整功能 + 配置管理）
        Server.MapController<StarAgentApiController>("/api");

        // 静态文件（NewLife.Agent.WebPanel 的嵌入式资源，物理 wwwroot 优先）
        Server.MapEmbedded<AgentWebPanel>("/", "NewLife.Agent.WebPanel.wwwroot");
    }
    #endregion

    #region 扩展面板
    protected override List<PanelExtension> GetExtensions()
    {
        var list = base.GetExtensions();

        // Agent 配置管理面板
        list.Add(new PanelExtension
        {
            Id = "staragent-config",
            Name = "Agent 配置",
            Icon = "🚀",
            ApiEndpoint = "/api/staragentpanel",
            Mode = "html",
            Order = 10,
        });

        return list;
    }
    #endregion
}
#endif
