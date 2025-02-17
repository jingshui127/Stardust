﻿using System;
using System.Collections.Generic;
using NewLife.Cube;
using NewLife.Web;
using Stardust.Data;
using XCode.Membership;

namespace Stardust.Web.Areas.Registry.Controllers
{
    [RegistryArea]
    [Menu(0, false)]
    public class AppConsumeController : EntityController<AppConsume>
    {
        protected override IEnumerable<AppConsume> Search(Pager p)
        {
            //PageSetting.EnableAdd = false;
            //PageSetting.EnableNavbar = false;

            var appId = p["appId"].ToInt(-1);
            var serviceId = p["serviceId"].ToInt(-1);
            var enable = p["enable"]?.ToBoolean();

            return AppConsume.Search(appId, serviceId, enable, p["Q"], p);
        }
    }
}