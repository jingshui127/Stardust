using Microsoft.Extensions.DependencyInjection.Extensions;
using NewLife;
using NewLife.Common;
using XCode.Configuration;

namespace Stardust.Server.Services.ConfigSync;

/// <summary>集群配置同步选项</summary>
/// <remarks>
/// 收集配置项后延迟回放到 <see cref="ConfigSyncManager"/>。
/// NewLife.XCode 12.3.2026.1122-beta1034 起 <see cref="ConfigSyncManager"/> 构造接受应用标识字符串，
/// 此前的 <c>ConfigSyncOptions</c> 已并入管理器，此处为 Stardust 侧适配收集器。
/// </remarks>
public class ConfigSyncOptions
{
    private readonly List<Action<ConfigSyncManager>> _apply = [];

    /// <summary>应用标识</summary>
    public String AppId { get; set; } = null!;

    /// <summary>添加公共配置作用域</summary>
    public void AddCommon(String[]? skip = null) => _apply.Add(m => m.AddCommon(skip ?? []));

    /// <summary>添加指定类型配置作用域</summary>
    /// <param name="scope">作用域</param>
    /// <param name="skip">跳过的配置项</param>
    public void Add<T>(String scope, String[]? skip = null) where T : NewLife.Configuration.Config<T>, new() => _apply.Add(m => m.Add<T>(scope, skip ?? []));

    /// <summary>回放收集的配置到管理器</summary>
    internal void Apply(ConfigSyncManager manager)
    {
        foreach (var action in _apply) action(manager);
    }
}

/// <summary>集群配置同步扩展</summary>
/// <remarks>
/// 注册后由 <see cref="ConfigSyncManager"/> 定时比较 Parameter 表与本机配置文件的时间，
/// 决定是否用 Parameter 表整份覆盖本机配置文件，并通过事件总线把变更通知其它实例。
/// </remarks>
public static class StardustConfigSyncExtensions
{
    /// <summary>添加集群配置同步</summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">同步选项。指定应用标识与参与同步的配置作用域</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddConfigSync(this IServiceCollection services, Action<ConfigSyncOptions> configure)
    {
        var options = new ConfigSyncOptions();
        configure?.Invoke(options);

        // 未指定应用标识时取系统配置名称，与单机部署的默认值保持一致
        if (options.AppId.IsNullOrEmpty()) options.AppId = SysConfig.Current.Name;

        // NewLife.XCode 包中 ConfigSyncManager 构造接受应用标识，配置项通过 Add/AddCommon 回放
        var manager = new ConfigSyncManager(options.AppId);
        options.Apply(manager);

        services.AddSingleton(manager);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, StardustConfigSyncHostedService>());

        return services;
    }
}
