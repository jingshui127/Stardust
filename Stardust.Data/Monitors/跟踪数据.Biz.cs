﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife;
using NewLife.Data;
using NewLife.Log;
using Stardust.Data.Nodes;
using XCode;
using XCode.Membership;
using XCode.Shards;

namespace Stardust.Data.Monitors
{
    /// <summary>跟踪数据。应用定时上报采样得到的埋点跟踪原始数据</summary>
    public partial class TraceData : Entity<TraceData>
    {
        #region 对象操作
        static TraceData()
        {
            // 配置自动分表策略，一般在实体类静态构造函数中配置
            Meta.ShardPolicy = new TimeShardPolicy(nameof(Id), Meta.Factory)
            {
                ConnPolicy = "{0}",
                TablePolicy = "{0}_{1:yyyyMMdd}",
                Step = TimeSpan.FromDays(1),
            };

            // 累加字段，生成 Update xx Set Count=Count+1234 Where xxx
            //var df = Meta.Factory.AdditionalFields;
            //df.Add(nameof(AppId));

            // 过滤器 UserModule、TimeModule、IPModule
            Meta.Modules.Add<TimeModule>();
        }

        /// <summary>验证数据，通过抛出异常的方式提示验证失败。</summary>
        /// <param name="isNew">是否插入</param>
        public override void Valid(Boolean isNew)
        {
            // 如果没有脏数据，则不需要进行任何处理
            if (!HasDirty) return;

            var len = _.Name.Length;
            if (Name.Length > len) Name = Name.Cut(len);

            // 建议先调用基类方法，基类方法会做一些统一处理
            base.Valid(isNew);

            //StatDate = StartTime.ToDateTime().ToLocalTime().Date;
            if (TotalCost < 0) TotalCost = 0;
            if (!Dirtys[nameof(Cost)])
            {
                // 为了让平均值逼近TP99，避免毛刺干扰，减去最大值再取平均
                if (Total >= 50)
                    Cost = (Int32)Math.Round((Double)(TotalCost - MaxCost) / (Total - 1));
                else
                    Cost = Total == 0 ? 0 : (Int32)Math.Round((Double)TotalCost / Total);
            }
        }
        #endregion

        #region 扩展属性
        /// <summary>应用</summary>
        [XmlIgnore, IgnoreDataMember]
        public AppTracer App => Extends.Get(nameof(App), k => AppTracer.FindByID(AppId));

        /// <summary>应用</summary>
        [Map(nameof(AppId))]
        public String AppName => App + "";

        /// <summary>节点</summary>
        [XmlIgnore, ScriptIgnore, IgnoreDataMember]
        public Node Node => Extends.Get(nameof(Node), k => Node.FindByID(NodeId));

        /// <summary>节点</summary>
        [Map(__.NodeId)]
        public String NodeName => Node?.Name;

        /// <summary>关联项</summary>
        [XmlIgnore, IgnoreDataMember]
        public TraceData Link => Extends.Get(nameof(TraceData), k => FindById(LinkId));

        /// <summary>开始时间</summary>
        [Map(nameof(StartTime))]
        public DateTime Start => StartTime.ToDateTime().ToLocalTime();

        /// <summary>结束时间</summary>
        [Map(nameof(EndTime))]
        public DateTime End => EndTime.ToDateTime().ToLocalTime();
        #endregion

        #region 扩展查询
        /// <summary>根据编号查找</summary>
        /// <param name="id">编号</param>
        /// <returns>实体对象</returns>
        public static TraceData FindById(Int64 id)
        {
            if (id <= 0) return null;

            //// 实体缓存
            //if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.Id == id);

            // 单对象缓存
            return Meta.SingleCache[id];

            //return Find(_.Id == id);
        }

        ///// <summary>根据应用、操作名查找</summary>
        ///// <param name="appId">应用</param>
        ///// <param name="name">操作名</param>
        ///// <returns>实体列表</returns>
        //public static IList<TraceData> FindAllByAppIdAndName(Int32 appId, String name)
        //{
        //    //// 实体缓存
        //    //if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.AppId == appId && e.Name == name);

        //    return FindAll(_.AppId == appId & _.Name == name);
        //}
        #endregion

        #region 高级查询
        /// <summary>高级查询</summary>
        /// <param name="appId">应用</param>
        /// <param name="itemId">跟踪项</param>
        /// <param name="clientId">客户端实例</param>
        /// <param name="name">操作名。接口名或埋点名</param>
        /// <param name="kind">时间种类。day/hour/minute</param>
        /// <param name="minError">最小错误数。指定后，只返回错误数大于等于该值的数据</param>
        /// <param name="start">创建时间开始</param>
        /// <param name="end">创建时间结束</param>
        /// <param name="key">关键字</param>
        /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
        /// <returns>实体列表</returns>
        public static IList<TraceData> Search(Int32 appId, Int32 itemId, String clientId, String name, String kind, Int32 minError, DateTime start, DateTime end, String key, PageParameter page)
        {
            var exp = new WhereExpression();

            if (appId >= 0) exp &= _.AppId == appId;
            if (itemId > 0) exp &= _.ItemId == itemId;
            if (!clientId.IsNullOrEmpty()) exp &= _.ClientId == clientId;
            if (!name.IsNullOrEmpty()) exp &= _.Name == name;
            if (!key.IsNullOrEmpty()) exp &= _.ClientId == key | _.Name == key;
            if (minError > 0) exp &= _.Errors >= minError;

            if (appId > 0 && start.Year > 2000)
            {
                var fi = kind switch
                {
                    "day" => _.StatDate,
                    "hour" => _.StatHour,
                    "minute" => _.StatMinute,
                    _ => _.StatDate,
                };

                if (start == end)
                    exp &= fi == start;
                else
                    exp &= fi.Between(start, end);
            }
            else
                exp &= _.Id.Between(start, end, Meta.Factory.Snow);

            using var split = Meta.CreateShard(start);

            return FindAll(exp, page);
        }

        /// <summary>根据时间类型搜索</summary>
        /// <param name="appId"></param>
        /// <param name="itemId"></param>
        /// <param name="kind"></param>
        /// <param name="time"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static IList<TraceData> Search(Int32 appId, Int32 itemId, String kind, DateTime time, Int32 count)
        {
            var exp = new WhereExpression();

            if (appId >= 0) exp &= _.AppId == appId;
            if (itemId > 0) exp &= _.ItemId == itemId;

            var fi = kind switch
            {
                "day" => _.StatDate,
                "hour" => _.StatHour,
                "minute" => _.StatMinute,
                _ => _.StatDate,
            };
            exp &= fi == time;

            using var split = Meta.CreateShard(time);

            return FindAll(exp, new PageParameter { PageSize = count });
        }

        /// <summary>根据应用接口分组统计</summary>
        /// <param name="kind"></param>
        /// <param name="time"></param>
        /// <param name="appIds"></param>
        /// <returns></returns>
        public static IList<TraceData> SearchGroupAppAndName(String kind, DateTime time, Int32[] appIds)
        {
            var exp = new WhereExpression();
            var selects = _.Total.Sum() & _.Errors.Sum() & _.TotalCost.Sum() & _.MaxCost.Max() & _.MinCost.Min() & _.AppId & _.Name;

            var fi = kind switch
            {
                "day" => _.StatDate,
                "hour" => _.StatHour,
                "minute" => _.StatMinute,
                _ => _.StatDate,
            };
            exp &= fi == time;

            if (appIds != null && appIds.Length > 0) exp &= _.AppId.In(appIds);

            return FindAll(exp.GroupBy(_.AppId, _.Name), null, selects);
        }

        /// <summary>查询指定应用根据操作名分组统计</summary>
        /// <param name="appId"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static IList<TraceData> SearchGroupAppAndName(Int32 appId, DateTime start, DateTime end)
        {
            var selects = _.Total.Sum() & _.Errors.Sum() & _.TotalCost.Sum() & _.MaxCost.Max() & _.MinCost.Min() & _.AppId & _.Name & _.StatMinute;

            var exp = new WhereExpression();
            exp &= _.AppId == appId;
            exp &= _.StatMinute >= start & _.StatMinute <= end;

            using var split = Meta.CreateShard(start);

            return FindAll(exp.GroupBy(_.AppId, _.Name, _.StatMinute), null, selects);
        }

        /// <summary>根据应用分组统计</summary>
        /// <param name="date"></param>
        /// <param name="appIds"></param>
        /// <returns></returns>
        public static IList<TraceData> SearchGroupApp(DateTime date, Int32[] appIds)
        {
            var selects = _.Total.Sum() & _.Errors.Sum() & _.TotalCost.Sum() & _.MaxCost.Max() & _.MinCost.Min() & _.AppId;
            var where = new WhereExpression() & _.StatDate == date;
            if (appIds != null && appIds.Length > 0) where &= _.AppId.In(appIds);

            return FindAll(where.GroupBy(_.AppId), null, selects);
        }

        /// <summary>修正当天数据</summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static Int32 FixStatDate(DateTime date)
        {
            return Update(_.StatDate == date, _.Id.Between(date, date, Meta.Factory.Snow));
        }
        #endregion

        #region 业务操作
        /// <summary>根据跟踪数据创建对象</summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static TraceData Create(ISpanBuilder builder)
        {
            var time = builder.StartTime.ToDateTime().ToLocalTime();

            var td = new TraceData
            {
                Id = Meta.Factory.Snow.NewId(),
                StatDate = time.Date,
                StatHour = time.Date.AddHours(time.Hour),
                StatMinute = time.Date.AddHours(time.Hour).AddMinutes(time.Minute / 5 * 5),
                Name = builder.Name.Cut(_.Name.Length),
                StartTime = builder.StartTime,
                EndTime = builder.EndTime,

                Total = builder.Total,
                Errors = builder.Errors,
                TotalCost = builder.Cost,
                MaxCost = builder.MaxCost,
                MinCost = builder.MinCost,
            };

            if (builder.Samples != null) td.Samples = builder.Samples.Count;
            if (builder.ErrorSamples != null) td.ErrorSamples = builder.ErrorSamples.Count;

            // 清空异常耗时数据
            if (td.TotalCost < 0) td.TotalCost = 0;

            return td;
        }

        /// <summary>删除指定日期之前的数据</summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static Int32 DeleteBefore(DateTime date)
        {
            //Delete(_.Id < Meta.Factory.Snow.GetId(date));

            var snow = Meta.Factory.Snow;
            var whereExp = _.Id < snow.GetId(date);

            // 使用底层接口，加大执行时间
            var session = Meta.Session;
            using var cmd = session.Dal.Session.CreateCommand($"Delete From {session.FormatedTableName} Where {whereExp}");
            cmd.CommandTimeout = 5 * 60;
            return session.Dal.Session.Execute(cmd);
        }
        #endregion
    }
}