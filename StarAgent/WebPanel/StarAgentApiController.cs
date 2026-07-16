#if !NET40
using System.ComponentModel;
using System.Reflection;
using NewLife;
using NewLife.Agent.WebPanel;
using NewLife.Serialization;
using Stardust.Models;

namespace StarAgent.WebPanel;

/// <summary>StarAgent 扩展 API 控制器（继承 ApiController，复用完整功能 + 强类型配置管理）</summary>
/// <remarks>
/// 继承 NewLife.Agent.WebPanel.ApiController，自动拥有 Status/Control/Logs/Health 等完整功能。
/// 额外添加配置管理方法，直接通过 StarAgentSetting.Current 强类型操作，避免 XML 字符串处理。
/// </remarks>
public class StarAgentApiController : ApiController
{
    #region 配置管理 API

    /// <summary>获取 Agent 配置面板（HTML + JavaScript，通过扩展机制加载）</summary>
    public Object StarAgentPanel()
    {
        if (!CheckAuth()) return new { code = 401, message = "Unauthorized" };

        var html = _panelHtml.Value;
        return new { code = 0, data = new { html } };
    }

    /// <summary>获取配置（强类型 JSON，包含字段元数据和服务列表）</summary>
    public Object GetAgentConfig()
    {
        if (!CheckAuth()) return new { code = 401, message = "Unauthorized" };

        var set = GetAgentSetting();
        if (set == null) return new { code = 500, message = "无法获取配置实例" };

        var fields = GetFieldsMetadata(set);
        var services = (Array)set.GetType().GetProperty("Services")?.GetValue(set) ?? Array.Empty<Object>();

        var svcList = new List<Dictionary<String, String>>();
        foreach (var s in services)
        {
            svcList.Add(ServiceInfoToDict(s));
        }

        return new { code = 0, data = new { fields, services = svcList } };
    }

    /// <summary>保存配置（接收 JSON，直接操作 StarAgentSetting.Current）</summary>
    /// <param name="content">JSON: {fields:[{name,value}], services:[{...}]}</param>
    public Object SaveAgentConfig(String content)
    {
        if (!CheckAuth()) return new { code = 401, message = "Unauthorized" };
        if (content.IsNullOrEmpty()) return new { code = 400, message = "配置内容不能为空" };

        try
        {
            var json = JsonParser.Decode(content);
            var set = GetAgentSetting();
            if (set == null) return new { code = 500, message = "无法获取配置实例" };

            var setType = set.GetType();

            // 更新普通字段
            var fields = json["fields"] as IList<Object>;
            if (fields != null)
            {
                foreach (var fObj in fields)
                {
                    var f = fObj as IDictionary<String, Object>;
                    var name = f["name"]?.ToString();
                    var value = f["value"]?.ToString();
                    if (name.IsNullOrEmpty() || name == "Services") continue;

                    var prop = setType.GetProperty(name);
                    if (prop == null || !prop.CanWrite) continue;

                    var converted = ConvertValue(value, prop.PropertyType);
                    if (converted != null) prop.SetValue(set, converted);
                }
            }

            // 更新服务列表
            var services = json["services"] as IList<Object>;
            if (services != null)
            {
                var list = new List<ServiceInfo>();
                foreach (var svcObj in services)
                {
                    var svc = svcObj as IDictionary<String, Object>;
                    var info = new ServiceInfo();
                    foreach (var kv in svc)
                    {
                        SetServiceInfoField(info, kv.Key, kv.Value?.ToString());
                    }
                    list.Add(info);
                }
                var servicesProp = setType.GetProperty("Services");
                servicesProp?.SetValue(set, list.ToArray());
            }

            // 调用 Save() 方法
            var saveMethod = setType.GetMethod("Save", Type.EmptyTypes);
            saveMethod?.Invoke(set, null);

            return new { code = 0, message = "配置已保存，文件变更将自动重载" };
        }
        catch (Exception ex)
        {
            return new { code = 500, message = "保存失败：" + ex.Message };
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>通过反射获取 StarAgentSetting.Current（避免循环依赖）</summary>
    private Object GetAgentSetting()
    {
        var service = Service;
        if (service == null) return null;

        var prop = service.GetType().GetProperty("AgentSetting");
        return prop?.GetValue(service);
    }

    private List<Object> GetFieldsMetadata(Object set)
    {
        var list = new List<Object>();
        var type = set.GetType();

        var editable = new HashSet<String> { "Debug", "Delay", "SyncTime", "StartupHook" };
        var systemFields = new HashSet<String> { "UserName", "Dpi", "Resolution" };

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.Name == "Services") continue;
            if (prop.GetIndexParameters().Length > 0) continue;

            var value = prop.GetValue(set);
            var desc = prop.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "";

            var category = editable.Contains(prop.Name) ? "editable" :
                          systemFields.Contains(prop.Name) ? "system" : "readonly";

            list.Add(new
            {
                name = prop.Name,
                value = value?.ToString() ?? "",
                comment = desc,
                category,
                type = prop.PropertyType.Name.ToLower(),
            });
        }

        return list;
    }

    private Dictionary<String, String> ServiceInfoToDict(Object s)
    {
        var dict = new Dictionary<String, String>();
        if (s == null) return dict;

        var type = s.GetType();
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.GetIndexParameters().Length > 0) continue;
            var v = prop.GetValue(s);
            dict[prop.Name] = v?.ToString() ?? "";
        }

        return dict;
    }

    private void SetServiceInfoField(ServiceInfo info, String fieldName, String value)
    {
        var type = info.GetType();
        var prop = type.GetProperty(fieldName);
        if (prop == null || !prop.CanWrite) return;

        var converted = ConvertValue(value, prop.PropertyType);
        if (converted != null) prop.SetValue(info, converted);
    }

    private Object ConvertValue(String value, Type targetType)
    {
        try
        {
            if (targetType == typeof(String)) return value ?? "";
            if (targetType == typeof(Boolean)) return value.EqualIgnoreCase("true");
            if (targetType == typeof(Int32)) return Int32.TryParse(value, out var i) ? i : 0;
            if (targetType == typeof(Int64)) return Int64.TryParse(value, out var l) ? l : 0L;
            if (targetType == typeof(Double)) return Double.TryParse(value, out var d) ? d : 0.0;
            if (targetType.IsEnum) return Enum.Parse(targetType, value, true);
            return value;
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region 配置面板 HTML（懒加载，从嵌入式资源读取）

    private static readonly Lazy<String> _panelHtml = new(() =>
    {
        var asm = typeof(StarAgentApiController).Assembly;
        var resourceName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("staragent_panel.html", StringComparison.OrdinalIgnoreCase));
        if (resourceName.IsNullOrEmpty()) return "<p>面板资源未找到</p>";

        using var stream = asm.GetManifestResourceStream(resourceName);
        if (stream == null) return "<p>面板资源流未找到</p>";
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    });

    #endregion
}
#endif
