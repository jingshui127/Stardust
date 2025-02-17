﻿using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Cube;
using NewLife.Cube.ViewModels;
using NewLife.Web;
using Stardust.Data.Deployment;
using XCode.Membership;
using Attachment = NewLife.Cube.Entity.Attachment;

namespace Stardust.Web.Areas.Deployment.Controllers;

[Menu(80)]
[DeploymentArea]
public class AppDeployVersionController : EntityController<AppDeployVersion>
{
    static AppDeployVersionController()
    {
        ListFields.RemoveCreateField();
        ListFields.RemoveRemarkField();

        AddFormFields.RemoveCreateField();
        AddFormFields.RemoveField("Hash");

        LogOnChange = true;

        {
            var df = ListFields.GetField("AppName") as ListField;
            df.Url = "/Deployment/AppDeploy?Id={AppId}";
        }
        {
            var df = ListFields.AddListField("UseVersion", null, "Enable");
            df.Header = "使用版本";
            df.DisplayName = "使用版本";
            df.Title = "应用部署集使用该版本";
            df.Url = "/Deployment/AppDeployVersion/UseVersion?Id={Id}";
            df.DataAction = "action";
        }
    }

    protected override IEnumerable<AppDeployVersion> Search(Pager p)
    {
        var id = p["id"].ToInt(-1);
        if (id > 0)
        {
            var entity = AppDeployVersion.FindById(id);
            if (entity != null) return new List<AppDeployVersion> { entity };
        }

        var appId = p["appId"].ToInt(-1);
        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();

        PageSetting.EnableAdd = appId > 0;
        PageSetting.EnableNavbar = false;

        return AppDeployVersion.Search(appId, null, null, start, end, p["Q"], p);
    }

    protected override Boolean Valid(AppDeployVersion entity, DataObjectMethodType type, Boolean post)
    {
        if (!post && type == DataObjectMethodType.Insert) entity.Version = DateTime.Now.ToString("yyyyMMdd-HHmmss");

        return base.Valid(entity, type, post);
    }

    protected override Int32 OnInsert(AppDeployVersion entity)
    {
        var rs = base.OnInsert(entity);
        entity.App?.Fix();
        return rs;
    }

    protected override Int32 OnUpdate(AppDeployVersion entity)
    {
        var rs = base.OnUpdate(entity);
        entity.App?.Fix();
        return rs;
    }

    protected override Int32 OnDelete(AppDeployVersion entity)
    {
        var rs = base.OnDelete(entity);
        entity.App?.Fix();
        return rs;
    }

    protected override async Task<Attachment> SaveFile(AppDeployVersion entity, IFormFile file, String uploadPath, String fileName)
    {
        var att = await base.SaveFile(entity, file, uploadPath, fileName);
        if (att != null)
        {
            entity.Hash = att.Hash;
            entity.Url = $"/cube/file?id={att.Id}{att.Extension}";

            entity.Update();
        }

        // 不给上层拿到附件，避免Url字段被覆盖
        return null;
    }

    [EntityAuthorize(PermissionFlags.Update)]
    public ActionResult UseVersion(Int32 id)
    {
        var ver = AppDeployVersion.FindById(id);
        if (ver == null) throw new Exception("找不到版本！");

        if (!ver.Enable) throw new Exception("版本未启用！");
        if (ver.Version.IsNullOrEmpty()) throw new Exception("版本号未设置！");
        if (ver.Url.IsNullOrEmpty()) throw new Exception("文件不存在！");

        var app = ver.App;
        app.Version = ver.Version;
        app.Update();

        return JsonRefresh($"成功！");
    }
}