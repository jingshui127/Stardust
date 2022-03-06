﻿using System;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Cube;
using NewLife.Remoting;
using Stardust.Data;
using Stardust.Data.Configs;
using Stardust.Models;
using Stardust.Server.Common;
using Stardust.Server.Services;
using XCode.Membership;

namespace Stardust.Web.Controllers
{
    /// <summary>配置中心服务。向应用提供配置服务</summary>
    [Route("[controller]/[action]")]
    public class ConfigController : ControllerBase
    {
        private readonly ConfigService _configService;
        private readonly TokenService _tokenService;

        public ConfigController(ConfigService configService, TokenService tokenService)
        {
            _configService = configService;
            _tokenService = tokenService;
        }

        [ApiFilter]
        public ConfigInfo GetAll(String appId, String secret, String scope, Int32 version)
        {
            if (appId.IsNullOrEmpty()) throw new ArgumentNullException(nameof(appId));
            if (ManageProvider.User == null) throw new ApiException(403, "未登录！");

            // 验证
            var app = Valid(appId, secret, out var online);
            var ip = HttpContext.GetUserHost();

            // 版本没有变化时，不做计算处理，不返回配置数据
            if (version >= app.Version) return new ConfigInfo { Version = app.Version, UpdateTime = app.UpdateTime };

            // 作用域为空时重写
            scope = scope.IsNullOrEmpty() ? AppRule.CheckScope(app.Id, ip, null) : scope;
            online.Scope = scope;

            var dic = _configService.GetConfigs(app, scope);

            // 返回WorkerId
            if (app.EnableWorkerId && dic.ContainsKey(_configService.WorkerIdName))
                dic[_configService.WorkerIdName] = online.WorkerId + "";

            return new ConfigInfo
            {
                Version = app.Version,
                Scope = scope,
                SourceIP = ip,
                NextVersion = app.NextVersion,
                NextPublish = app.PublishTime.ToFullString(""),
                UpdateTime = app.UpdateTime,
                Configs = dic,
            };
        }

        private AppConfig Valid(String appId, String secret, out AppOnline online)
        {
            var ap = _tokenService.Authorize(appId, secret, false);

            var app = AppConfig.FindByName(appId);
            if (app == null) app = AppConfig.Find(AppConfig._.Name == appId);
            if (app == null)
            {
                app = new AppConfig
                {
                    Name = ap.Name,
                    Enable = ap.Enable,
                };

                app.Insert();
            }

            // 更新心跳信息
            var ip = HttpContext.GetUserHost();
            online = AppOnline.UpdateOnline(ap, null, ip, appId);

            // 检查应用有效性
            if (!app.Enable) throw new ArgumentOutOfRangeException(nameof(appId), $"应用[{appId}]已禁用！");

            // 刷新WorkerId
            if (app.EnableWorkerId && online.WorkerId <= 0) _configService.RefreshWorkerId(app, online);

            return app;
        }
    }
}