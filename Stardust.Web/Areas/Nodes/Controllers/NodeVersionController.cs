﻿using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Cube;
using Stardust.Data.Deployment;
using Stardust.Data.Nodes;
using XCode.Membership;
using NewLife.Cube.Entity;
using Attachment = NewLife.Cube.Entity.Attachment;
using NewLife.Cube.ViewModels;

namespace Stardust.Web.Areas.Nodes.Controllers;

[Menu(89)]
[NodesArea]
public class NodeVersionController : EntityController<NodeVersion>
{
    static NodeVersionController()
    {
        LogOnChange = true;

        {
            var df = ListFields.GetField("Source") as ListField;
            //df.DisplayName = "下载";
            df.Url = "{Source}";
            //df.DataVisible = e =>
            //{
            //    var entity = e as NodeVersion;
            //    return !entity.Source.IsNullOrEmpty() && !entity.Source.StartsWithIgnoreCase("http://", "https://");
            //};
        }

        {
            var df = ListFields.AddListField("Log", "CreateUserID");
            df.DisplayName = "修改日志";
            df.Header = "修改日志";
            df.Url = "/Admin/Log?category=节点版本&linkId={ID}";
        }
    }

    protected override async Task<Attachment> SaveFile(NodeVersion entity, IFormFile file, String uploadPath, String fileName)
    {
        var att = await base.SaveFile(entity, file, uploadPath, fileName);
        if (att != null)
        {
            entity.FileHash = att.Hash;
            entity.Source = $"/cube/file?id={att.Id}{att.Extension}";

            entity.Update();
        }

        // 不给上层拿到附件，避免Url字段被覆盖
        return null;
    }

    //protected override async Task<IList<String>> SaveFiles(NodeVersion entity, String uploadPath = null)
    //{
    //    var rs = await base.SaveFiles(entity, uploadPath);

    //    // 更新文件哈希
    //    if (rs.Count > 0 && !entity.Source.IsNullOrEmpty())
    //    {
    //        var fi = NewLife.Cube.Setting.Current.UploadPath.CombinePath(entity.Source).AsFile();
    //        if (fi.Exists)
    //        {
    //            entity.FileHash = fi.ReadBytes().MD5().ToHex();
    //        }
    //    }

    //    return rs;
    //}

    public ActionResult GetVersion(String id)
    {
        var name = id;
        var nv = NodeVersion.FindByVersion(name.TrimEnd(".zip"));
        if (nv == null) return NotFound("非法参数");

        var set = NewLife.Cube.Setting.Current;
        var updatePath = set.UploadPath;
        var fi = updatePath.CombinePath(nv.Source).AsFile();
        if (!fi.Exists) return NotFound("文件不存在");

        return PhysicalFile(fi.FullName, "application/octet-stream", name);
    }
}