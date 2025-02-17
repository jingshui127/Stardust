﻿using NewLife;
using NewLife.Cube;
using NewLife.Cube.Extensions;
using NewLife.Cube.ViewModels;
using NewLife.Web;
using Stardust.Data;

namespace Stardust.Web.Areas.Registry.Controllers;

[RegistryArea]
[Menu(95)]
public class AppOnlineController : EntityController<AppOnline>
{
    static AppOnlineController()
    {
        ListFields.RemoveField("ProcessName", "MachineName", "UserName", "Token", "Compile");

        {
            var df = ListFields.GetField("NodeName") as ListField;
            df.Header = "节点";
            df.DisplayName = "{NodeName}";
            df.Url = "/Nodes/Node?Id={NodeId}";
        }
        {
            var df = ListFields.AddListField("Meter", "WebSocket");
            df.Header = "性能";
            df.DisplayName = "性能";
            df.Url = "/Registry/AppMeter?appId={AppId}&clientId={Client}";
        }
        {
            var df = ListFields.AddListField("History", "WebSocket");
            df.DisplayName = "历史";
            df.Url = "/Registry/AppHistory?appId={AppId}&client={Client}";
        }
        {
            var df = ListFields.GetField("TraceId") as ListField;
            df.DisplayName = "跟踪";
            df.Url = StarHelper.BuildUrl("{TraceId}");
            df.DataVisible = e => e is AppOnline entity && !entity.TraceId.IsNullOrEmpty();
        }
    }

    protected override IEnumerable<AppOnline> Search(Pager p)
    {
        var appId = p["appId"].ToInt(-1);
        var nodeId = p["nodeId"].ToInt(-1);
        var category = p["category"];

        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();

        return AppOnline.Search(appId, nodeId, category, start, end, p["Q"], p);
    }
}