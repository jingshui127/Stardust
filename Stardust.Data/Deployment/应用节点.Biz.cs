﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife;
using NewLife.Data;
using Stardust.Data.Nodes;
using Stardust.Models;
using XCode;
using XCode.Membership;

namespace Stardust.Data.Deployment;

/// <summary>部署节点。应用和节点服务器的依赖关系</summary>
public partial class AppDeployNode : Entity<AppDeployNode>
{
    #region 对象操作
    static AppDeployNode()
    {
        // 累加字段，生成 Update xx Set Count=Count+1234 Where xxx
        //var df = Meta.Factory.AdditionalFields;
        //df.Add(nameof(AppId));

        // 过滤器 UserModule、TimeModule、IPModule
        Meta.Modules.Add<UserModule>();
        Meta.Modules.Add<TimeModule>();
        Meta.Modules.Add<IPModule>();
    }

    /// <summary>验证并修补数据，通过抛出异常的方式提示验证失败。</summary>
    /// <param name="isNew">是否插入</param>
    public override void Valid(Boolean isNew)
    {
        // 如果没有脏数据，则不需要进行任何处理
        if (!HasDirty) return;

        if (AppId <= 0) throw new ArgumentNullException(nameof(AppId));
        if (NodeId <= 0) throw new ArgumentNullException(nameof(NodeId));

        var len = _.IP.Length;
        if (len > 0 && !IP.IsNullOrEmpty() && IP.Length > len)
        {
            // 取前三个
            var ss = IP.Split(',');
            IP = ss.Take(3).Join(",");
            if (IP.Length > len) IP = IP[..len];
        }

        base.Valid(isNew);
    }
    #endregion

    #region 扩展属性
    /// <summary>应用</summary>
    [XmlIgnore, ScriptIgnore, IgnoreDataMember]
    public AppDeploy App => Extends.Get(nameof(App), k => AppDeploy.FindById(AppId));

    /// <summary>应用</summary>
    [Map(__.AppId)]
    public String AppName => App?.Name;

    /// <summary>节点</summary>
    [XmlIgnore, ScriptIgnore, IgnoreDataMember]
    public Node Node => Extends.Get(nameof(Node), k => Node.FindByID(NodeId));

    /// <summary>节点</summary>
    [Map(__.NodeId)]
    public String NodeName => Node?.Name;
    #endregion

    #region 扩展查询
    /// <summary>根据编号查找</summary>
    /// <param name="id">编号</param>
    /// <returns>实体对象</returns>
    public static AppDeployNode FindById(Int32 id)
    {
        if (id <= 0) return null;

        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Id == id);

        // 单对象缓存
        return Meta.SingleCache[id];

        //return Find(_.Id == id);
    }

    /// <summary>根据应用查找</summary>
    /// <param name="appId">应用</param>
    /// <returns>实体列表</returns>
    public static IList<AppDeployNode> FindAllByAppId(Int32 appId)
    {
        if (appId <= 0) return new List<AppDeployNode>();

        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.AppId == appId);

        return FindAll(_.AppId == appId);
    }

    /// <summary>根据节点查找</summary>
    /// <param name="nodeId">节点</param>
    /// <returns>实体列表</returns>
    public static IList<AppDeployNode> FindAllByNodeId(Int32 nodeId)
    {
        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.NodeId == nodeId);

        return FindAll(_.NodeId == nodeId);
    }
    #endregion

    #region 高级查询
    /// <summary>高级查询</summary>
    /// <param name="appId">应用。原始应用</param>
    /// <param name="nodeId">节点。节点服务器</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <returns>实体列表</returns>
    public static IList<AppDeployNode> Search(Int32 appId, Int32 nodeId, String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (appId >= 0) exp &= _.AppId == appId;
        if (nodeId >= 0) exp &= _.NodeId == nodeId;
        if (!key.IsNullOrEmpty()) exp &= _.CreateIP.Contains(key);

        return FindAll(exp, page);
    }

    /// <summary>高级查询</summary>
    /// <param name="appIds">应用。原始应用</param>
    /// <param name="nodeId">节点。节点服务器</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <returns></returns>
    public static IList<AppDeployNode> Search(Int32[] appIds, Int32 nodeId, String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (appIds != null && appIds.Length > 0) exp &= _.AppId.In(appIds);
        if (nodeId >= 0) exp &= _.NodeId == nodeId;
        if (!key.IsNullOrEmpty()) exp &= _.CreateIP.Contains(key);

        return FindAll(exp, page);
    }
    #endregion

    #region 业务操作
    /// <summary>
    /// 转应用服务信息
    /// </summary>
    /// <returns></returns>
    public ServiceInfo ToService()
    {
        var app = App;
        if (app == null) return null;

        var inf = new ServiceInfo
        {
            Name = app.Name,
            FileName = app.FileName,
            Arguments = app.Arguments,
            WorkingDirectory = app.WorkingDirectory,

            Enable = app.Enable && Enable,
            //AutoStart = app.AutoStart,
            //AutoStop = app.AutoStop,
            MaxMemory = app.MaxMemory,
            Mode = app.Mode,
        };

        return inf;
    }
    #endregion
}