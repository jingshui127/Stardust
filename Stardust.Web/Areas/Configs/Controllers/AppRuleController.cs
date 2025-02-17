﻿using NewLife.Cube;
using Stardust.Data.Configs;

namespace Stardust.Web.Areas.Configs.Controllers
{
    [ConfigsArea]
    public class AppRuleController : EntityController<AppRule>
    {
        static AppRuleController()
        {
            LogOnChange = true;

            {
                var df = ListFields.AddListField("Log", "CreateUserID");
                df.DisplayName = "修改日志";
                df.Header = "修改日志";
                df.Url = "/Admin/Log?category=应用规则&linkId={Id}";
            }
        }

        //protected override IEnumerable<AppRule> Search(Pager p)
        //{
        //    var appId = p["appId"].ToInt(-1);

        //    var start = p["dtStart"].ToDateTime();
        //    var end = p["dtEnd"].ToDateTime();

        //    return AppRule.Search(appId, start, end, p["Q"], p);
        //}
    }
}