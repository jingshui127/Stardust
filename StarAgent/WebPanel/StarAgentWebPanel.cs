using System.Reflection;
using NewLife;
using NewLife.Agent;
using NewLife.Agent.WebPanel;
using NewLife.Http;
using NewLife.Log;
using NewLife.Serialization;
using Stardust.Models;

namespace StarAgent.WebPanel;

public class StarAgentWebPanel : AgentWebPanel
{
    public StarAgentWebPanel(ServiceBase service) : base(service) { }

    protected override void RegisterRoutes()
    {
        base.RegisterRoutes();
        Server.MapController<StarAgentApiController>("/api");
    }
}

public class StarAgentApiController : IHttpController
{
    public IHttpContext Context { get; set; }

    public Object GetStarAgentConfig()
    {
        if (!CheckAuth()) return new { code = 401, message = "Unauthorized" };

        var set = StarAgentSetting.Current;
        var items = new List<Object>();

        foreach (var prop in typeof(StarAgentSetting).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || !prop.CanWrite) continue;
            if (prop.Name.EqualIgnoreCase(nameof(set.Secret))) continue;
            if (prop.PropertyType.IsArray && prop.PropertyType.GetElementType()?.Name == "ServiceInfo") continue;

            var displayName = prop.GetCustomAttribute<System.ComponentModel.DisplayNameAttribute>()?.DisplayName;
            if (displayName.IsNullOrEmpty())
            {
                var desc = prop.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description;
                if (!desc.IsNullOrEmpty())
                {
                    var dot = desc.IndexOf('。');
                    displayName = dot > 0 ? desc[..dot] : desc;
                }
            }
            displayName ??= prop.Name;

            var description = prop.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description ?? "";

            var typeName = prop.PropertyType.Name switch
            {
                "String" => "String",
                "Int32" => "Int32",
                "Boolean" => "Boolean",
                _ => prop.PropertyType.Name
            };

            items.Add(new
            {
                name = prop.Name,
                displayName,
                description,
                type = typeName,
                value = prop.GetValue(set, null)
            });
        }

        return new { code = 0, data = new { items } };
    }

    public Object UpdateStarAgentConfig(IDictionary<String, Object> updates)
    {
        if (!CheckAuth()) return new { code = 401, message = "Unauthorized" };

        if (updates == null || updates.Count == 0)
            return new { code = 400, message = "Missing config" };

        try
        {
            var set = StarAgentSetting.Current;

            foreach (var kv in updates)
            {
                var prop = typeof(StarAgentSetting).GetProperty(kv.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop == null || !prop.CanWrite) continue;

                if (kv.Key.EqualIgnoreCase(nameof(set.Secret))) continue;

                var value = kv.Value;
                var pt = prop.PropertyType;

                try
                {
                    if (pt == typeof(String))
                        prop.SetValue(set, value?.ToString());
                    else if (pt == typeof(Int32))
                        prop.SetValue(set, value.ToInt());
                    else if (pt == typeof(Boolean))
                        prop.SetValue(set, value.ToBoolean());
                    else
                        prop.SetValue(set, value);
                }
                catch { }
            }

            set.Save();

            return new { code = 0, message = "配置已保存，部分配置需重启后生效" };
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
            return new { code = 500, message = "保存失败：" + ex.Message };
        }
    }

    public Object GetServices()
    {
        if (!CheckAuth()) return new { code = 401, message = "Unauthorized" };

        var set = StarAgentSetting.Current;
        var services = set.Services ?? [];

        var list = services.Select(s => new
        {
            name = s.Name,
            fileName = s.FileName,
            arguments = s.Arguments,
            workingDirectory = s.WorkingDirectory,
            enable = s.Enable,
            mode = s.Mode,
            allowMultiple = s.AllowMultiple,
            autoStop = s.AutoStop,
            reloadOnChange = s.ReloadOnChange,
            maxMemory = s.MaxMemory,
            priority = s.Priority,
            healthCheck = s.HealthCheck
        }).ToList();

        return new { code = 0, data = new { services = list } };
    }

    public Object UpdateServices(Object data)
    {
        if (!CheckAuth()) return new { code = 401, message = "Unauthorized" };

        try
        {
            var json = data as IDictionary<String, Object>;
            if (json == null || !json.ContainsKey("services"))
                return new { code = 400, message = "Missing services" };

            var servicesJson = json["services"] as List<Object>;
            if (servicesJson == null)
                return new { code = 400, message = "Invalid services format" };

            var services = new List<ServiceInfo>();
            foreach (var svcJson in servicesJson)
            {
                var svcDict = svcJson as IDictionary<String, Object>;
                if (svcDict == null) continue;

                var svc = new ServiceInfo
                {
                    Name = svcDict.ContainsKey("name") ? svcDict["name"].ToString() : "",
                    FileName = svcDict.ContainsKey("fileName") ? svcDict["fileName"].ToString() : "",
                    Arguments = svcDict.ContainsKey("arguments") ? svcDict["arguments"].ToString() : "",
                    WorkingDirectory = svcDict.ContainsKey("workingDirectory") ? svcDict["workingDirectory"].ToString() : "",
                    Enable = svcDict.ContainsKey("enable") && svcDict["enable"].ToBoolean(),
                    Mode = svcDict.ContainsKey("mode") && Enum.TryParse(svcDict["mode"].ToString(), out DeployMode dm) ? dm : DeployMode.Default,
                    AllowMultiple = svcDict.ContainsKey("allowMultiple") && svcDict["allowMultiple"].ToBoolean(),
                    AutoStop = svcDict.ContainsKey("autoStop") && svcDict["autoStop"].ToBoolean(),
                    ReloadOnChange = svcDict.ContainsKey("reloadOnChange") && svcDict["reloadOnChange"].ToBoolean(),
                    MaxMemory = svcDict.ContainsKey("maxMemory") ? svcDict["maxMemory"].ToInt() : 0,
                    Priority = svcDict.ContainsKey("priority") && Enum.TryParse(svcDict["priority"].ToString(), out ProcessPriority pp) ? pp : ProcessPriority.Normal,
                    HealthCheck = svcDict.ContainsKey("healthCheck") ? svcDict["healthCheck"].ToString() : ""
                };
                services.Add(svc);
            }

            var set = StarAgentSetting.Current;
            set.Services = services.ToArray();
            set.Save();

            return new { code = 0, message = "服务配置已保存，需重启服务后生效" };
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
            return new { code = 500, message = "保存失败：" + ex.Message };
        }
    }

    private Boolean CheckAuth()
    {
        var panel = AgentWebPanel.Current;
        if (panel == null) return false;

        var token = Context.Request.Headers["Authorization"];
        if (!token.IsNullOrEmpty() && token.StartsWithIgnoreCase("Bearer "))
            token = token.Substring(7);

        return panel.ValidateToken(token);
    }
}
