﻿using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Cube;
using NewLife.Data;
using NewLife.Web;
using Stardust.Data.Nodes;
using XCode.Membership;
using static Stardust.Data.Nodes.Node;

namespace Stardust.Web.Areas.Nodes.Controllers
{
    [Menu(90)]
    [NodesArea]
    public class NodeController : EntityController<Node>
    {
        private readonly StarFactory _starFactory;

        static NodeController()
        {
            //ListFields.RemoveField("Secret", "ProductCode", "CompileTime", "OSVersion", "Architecture", "Dpi", "Resolution", "Processor", "CpuID", "Uuid", "MachineGuid", "DiskID", "MACS", "InstallPath", "Runtime", "Framework", "ProvinceID");

            var list = ListFields;
            list.Clear();
            var allows = new[] { "ID", "Name", "Code", "Category", "ProductCode", "CityName", "Enable", "Version", "Runtime", "IP", "OS", "MachineName", "Cpu", "Memory", "TotalSize", "Logins", "LastLogin", "OnlineTime", "UpdateTime", "UpdateIP" };
            foreach (var item in allows)
            {
                list.AddListField(item);
            }

            {
                var df = ListFields.AddListField("App", "Version");
                df.DisplayName = "应用实例";
                df.Url = "/Registry/AppOnline?nodeId={ID}";
            }
            {
                var df = ListFields.AddListField("DeployNodes", "Version");
                df.DisplayName = "部署实例";
                df.Url = "/Deployment/AppDeployNode?nodeId={ID}";
            }
            {
                var df = ListFields.AddListField("Meter", "Version");
                df.DisplayName = "性能";
                df.Url = "/Nodes/NodeData?nodeId={ID}";
            }
            {
                var df = ListFields.AddListField("History", "Version");
                df.DisplayName = "历史";
                df.Url = "/Nodes/NodeHistory?nodeId={ID}";
            }
            {
                var df = ListFields.AddListField("Log", "Version");
                df.DisplayName = "日志";
                df.Url = "/Admin/Log?category=节点&linkId={ID}";
            }
        }

        public NodeController(StarFactory starFactory)
        {
            LogOnChange = true;

            _starFactory = starFactory;
        }

        protected override IEnumerable<Node> Search(Pager p)
        {
            var nodeId = p["Id"].ToInt(-1);
            if (nodeId > 0)
            {
                var node = Node.FindByKey(nodeId);
                if (node != null) return new[] { node };
            }

            var rids = p["areaId"].SplitAsInt("/");
            var provinceId = rids.Length > 0 ? rids[0] : -1;
            var cityId = rids.Length > 1 ? rids[1] : -1;

            var category = p["category"];
            var product = p["product"];
            var version = p["version"];
            var enable = p["enable"]?.ToBoolean();

            var start = p["dtStart"].ToDateTime();
            var end = p["dtEnd"].ToDateTime();

            return Node.Search(provinceId, cityId, category, product, version, enable, start, end, p["Q"], p);
        }

        /// <summary>搜索</summary>
        /// <param name="category"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public ActionResult NodeSearch(String category, String key = null)
        {
            var page = new PageParameter { PageSize = 20 };

            // 默认排序
            if (page.Sort.IsNullOrEmpty()) page.Sort = _.Name;

            var list = SearchByCategory(category, true, key, page);

            return Json(0, null, list.Select(e => new
            {
                e.ID,
                e.Code,
                e.Name,
                e.Category,
            }).ToArray());
        }

        public async Task<ActionResult> Trace(Int32 id)
        {
            var node = FindByID(id);
            if (node != null)
            {
                //NodeCommand.Add(node, "截屏");
                //NodeCommand.Add(node, "抓日志");

                await _starFactory.SendNodeCommand(node.Code, "截屏");
                await _starFactory.SendNodeCommand(node.Code, "抓日志");
            }

            return RedirectToAction("Index");
        }

        [EntityAuthorize(PermissionFlags.Update)]
        public ActionResult SetAlarm(Boolean enable = true)
        {
            foreach (var item in SelectKeys)
            {
                var dt = FindByID(item.ToInt());
                if (dt != null)
                {
                    dt.AlarmOnOffline = enable;
                    dt.Save();
                }
            }

            return JsonRefresh("操作成功！");
        }
        [EntityAuthorize(PermissionFlags.Update)]
        public ActionResult ResetAlarm(Int32 alarmRate = 0)
        {
            foreach (var item in SelectKeys)
            {
                var dt = FindByID(item.ToInt());
                if (dt != null)
                {
                    dt.AlarmCpuRate = alarmRate;
                    dt.AlarmMemoryRate = alarmRate;
                    dt.AlarmDiskRate = alarmRate;
                    dt.Save();
                }
            }

            return JsonRefresh("操作成功！");
        }
    }
}