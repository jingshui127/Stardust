﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace Stardust.Data.Monitors
{
    /// <summary>采样数据2。采样备份，用于链路分析以及异常追踪</summary>
    [Serializable]
    [DataObject]
    [Description("采样数据2。采样备份，用于链路分析以及异常追踪")]
    [BindIndex("IX_SampleData2_TraceId", false, "TraceId")]
    [BindTable("SampleData2", Description = "采样数据2。采样备份，用于链路分析以及异常追踪", ConnName = "StardustData", DbType = DatabaseType.None)]
    public partial class SampleData2
    {
        #region 属性
        private Int64 _Id;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [Description("编号")]
        [DataObjectField(true, false, false, 0)]
        [BindColumn("Id", "编号", "")]
        public Int64 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

        private Int64 _DataId;
        /// <summary>数据</summary>
        [DisplayName("数据")]
        [Description("数据")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("DataId", "数据", "")]
        public Int64 DataId { get => _DataId; set { if (OnPropertyChanging("DataId", value)) { _DataId = value; OnPropertyChanged("DataId"); } } }

        private Int32 _AppId;
        /// <summary>应用</summary>
        [DisplayName("应用")]
        [Description("应用")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("AppId", "应用", "")]
        public Int32 AppId { get => _AppId; set { if (OnPropertyChanging("AppId", value)) { _AppId = value; OnPropertyChanged("AppId"); } } }

        private Int32 _ItemId;
        /// <summary>跟踪项</summary>
        [DisplayName("跟踪项")]
        [Description("跟踪项")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("ItemId", "跟踪项", "")]
        public Int32 ItemId { get => _ItemId; set { if (OnPropertyChanging("ItemId", value)) { _ItemId = value; OnPropertyChanged("ItemId"); } } }

        private String _Name;
        /// <summary>操作名。接口名或埋点名</summary>
        [DisplayName("操作名")]
        [Description("操作名。接口名或埋点名")]
        [DataObjectField(false, false, true, 200)]
        [BindColumn("Name", "操作名。接口名或埋点名", "", Master = true)]
        public String Name { get => _Name; set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } } }

        private Boolean _Success;
        /// <summary>正常</summary>
        [DisplayName("正常")]
        [Description("正常")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Success", "正常", "")]
        public Boolean Success { get => _Success; set { if (OnPropertyChanging("Success", value)) { _Success = value; OnPropertyChanged("Success"); } } }

        private Int64 _StartTime;
        /// <summary>开始时间。Unix毫秒</summary>
        [DisplayName("开始时间")]
        [Description("开始时间。Unix毫秒")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("StartTime", "开始时间。Unix毫秒", "")]
        public Int64 StartTime { get => _StartTime; set { if (OnPropertyChanging("StartTime", value)) { _StartTime = value; OnPropertyChanged("StartTime"); } } }

        private Int64 _EndTime;
        /// <summary>结束时间。Unix毫秒</summary>
        [DisplayName("结束时间")]
        [Description("结束时间。Unix毫秒")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("EndTime", "结束时间。Unix毫秒", "")]
        public Int64 EndTime { get => _EndTime; set { if (OnPropertyChanging("EndTime", value)) { _EndTime = value; OnPropertyChanged("EndTime"); } } }

        private Int32 _Cost;
        /// <summary>耗时。毫秒</summary>
        [DisplayName("耗时")]
        [Description("耗时。毫秒")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("Cost", "耗时。毫秒", "")]
        public Int32 Cost { get => _Cost; set { if (OnPropertyChanging("Cost", value)) { _Cost = value; OnPropertyChanged("Cost"); } } }

        private String _ClientId;
        /// <summary>实例。应用可能多实例部署，ip@proccessid</summary>
        [DisplayName("实例")]
        [Description("实例。应用可能多实例部署，ip@proccessid")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("ClientId", "实例。应用可能多实例部署，ip@proccessid", "")]
        public String ClientId { get => _ClientId; set { if (OnPropertyChanging("ClientId", value)) { _ClientId = value; OnPropertyChanged("ClientId"); } } }

        private String _TraceId;
        /// <summary>追踪。可用于关联多个片段，建立依赖关系，随线程上下文、Http、Rpc传递</summary>
        [DisplayName("追踪")]
        [Description("追踪。可用于关联多个片段，建立依赖关系，随线程上下文、Http、Rpc传递")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("TraceId", "追踪。可用于关联多个片段，建立依赖关系，随线程上下文、Http、Rpc传递", "")]
        public String TraceId { get => _TraceId; set { if (OnPropertyChanging("TraceId", value)) { _TraceId = value; OnPropertyChanged("TraceId"); } } }

        private String _SpanId;
        /// <summary>唯一标识。随线程上下文、Http、Rpc传递，作为内部片段的父级</summary>
        [DisplayName("唯一标识")]
        [Description("唯一标识。随线程上下文、Http、Rpc传递，作为内部片段的父级")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("SpanId", "唯一标识。随线程上下文、Http、Rpc传递，作为内部片段的父级", "")]
        public String SpanId { get => _SpanId; set { if (OnPropertyChanging("SpanId", value)) { _SpanId = value; OnPropertyChanged("SpanId"); } } }

        private String _ParentId;
        /// <summary>父级标识</summary>
        [DisplayName("父级标识")]
        [Description("父级标识")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("ParentId", "父级标识", "")]
        public String ParentId { get => _ParentId; set { if (OnPropertyChanging("ParentId", value)) { _ParentId = value; OnPropertyChanged("ParentId"); } } }

        private String _Tag;
        /// <summary>数据标签。记录一些附加数据</summary>
        [DisplayName("数据标签")]
        [Description("数据标签。记录一些附加数据")]
        [DataObjectField(false, false, true, 2000)]
        [BindColumn("Tag", "数据标签。记录一些附加数据", "")]
        public String Tag { get => _Tag; set { if (OnPropertyChanging("Tag", value)) { _Tag = value; OnPropertyChanged("Tag"); } } }

        private String _Error;
        /// <summary>错误信息</summary>
        [DisplayName("错误信息")]
        [Description("错误信息")]
        [DataObjectField(false, false, true, 2000)]
        [BindColumn("Error", "错误信息", "")]
        public String Error { get => _Error; set { if (OnPropertyChanging("Error", value)) { _Error = value; OnPropertyChanged("Error"); } } }

        private String _CreateUser;
        /// <summary>创建者</summary>
        [Category("扩展")]
        [DisplayName("创建者")]
        [Description("创建者")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("CreateUser", "创建者", "")]
        public String CreateUser { get => _CreateUser; set { if (OnPropertyChanging("CreateUser", value)) { _CreateUser = value; OnPropertyChanged("CreateUser"); } } }

        private Int32 _CreateUserID;
        /// <summary>创建者</summary>
        [Category("扩展")]
        [DisplayName("创建者")]
        [Description("创建者")]
        [DataObjectField(false, false, false, 0)]
        [BindColumn("CreateUserID", "创建者", "")]
        public Int32 CreateUserID { get => _CreateUserID; set { if (OnPropertyChanging("CreateUserID", value)) { _CreateUserID = value; OnPropertyChanged("CreateUserID"); } } }

        private String _CreateIP;
        /// <summary>创建地址</summary>
        [Category("扩展")]
        [DisplayName("创建地址")]
        [Description("创建地址")]
        [DataObjectField(false, false, true, 50)]
        [BindColumn("CreateIP", "创建地址", "")]
        public String CreateIP { get => _CreateIP; set { if (OnPropertyChanging("CreateIP", value)) { _CreateIP = value; OnPropertyChanged("CreateIP"); } } }

        private DateTime _CreateTime;
        /// <summary>创建时间</summary>
        [Category("扩展")]
        [DisplayName("创建时间")]
        [Description("创建时间")]
        [DataObjectField(false, false, true, 0)]
        [BindColumn("CreateTime", "创建时间", "")]
        public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        public override Object this[String name]
        {
            get
            {
                switch (name)
                {
                    case "Id": return _Id;
                    case "DataId": return _DataId;
                    case "AppId": return _AppId;
                    case "ItemId": return _ItemId;
                    case "Name": return _Name;
                    case "Success": return _Success;
                    case "StartTime": return _StartTime;
                    case "EndTime": return _EndTime;
                    case "Cost": return _Cost;
                    case "ClientId": return _ClientId;
                    case "TraceId": return _TraceId;
                    case "SpanId": return _SpanId;
                    case "ParentId": return _ParentId;
                    case "Tag": return _Tag;
                    case "Error": return _Error;
                    case "CreateUser": return _CreateUser;
                    case "CreateUserID": return _CreateUserID;
                    case "CreateIP": return _CreateIP;
                    case "CreateTime": return _CreateTime;
                    default: return base[name];
                }
            }
            set
            {
                switch (name)
                {
                    case "Id": _Id = value.ToLong(); break;
                    case "DataId": _DataId = value.ToLong(); break;
                    case "AppId": _AppId = value.ToInt(); break;
                    case "ItemId": _ItemId = value.ToInt(); break;
                    case "Name": _Name = Convert.ToString(value); break;
                    case "Success": _Success = value.ToBoolean(); break;
                    case "StartTime": _StartTime = value.ToLong(); break;
                    case "EndTime": _EndTime = value.ToLong(); break;
                    case "Cost": _Cost = value.ToInt(); break;
                    case "ClientId": _ClientId = Convert.ToString(value); break;
                    case "TraceId": _TraceId = Convert.ToString(value); break;
                    case "SpanId": _SpanId = Convert.ToString(value); break;
                    case "ParentId": _ParentId = Convert.ToString(value); break;
                    case "Tag": _Tag = Convert.ToString(value); break;
                    case "Error": _Error = Convert.ToString(value); break;
                    case "CreateUser": _CreateUser = Convert.ToString(value); break;
                    case "CreateUserID": _CreateUserID = value.ToInt(); break;
                    case "CreateIP": _CreateIP = Convert.ToString(value); break;
                    case "CreateTime": _CreateTime = value.ToDateTime(); break;
                    default: base[name] = value; break;
                }
            }
        }
        #endregion

        #region 字段名
        /// <summary>取得采样数据2字段信息的快捷方式</summary>
        public partial class _
        {
            /// <summary>编号</summary>
            public static readonly Field Id = FindByName("Id");

            /// <summary>数据</summary>
            public static readonly Field DataId = FindByName("DataId");

            /// <summary>应用</summary>
            public static readonly Field AppId = FindByName("AppId");

            /// <summary>跟踪项</summary>
            public static readonly Field ItemId = FindByName("ItemId");

            /// <summary>操作名。接口名或埋点名</summary>
            public static readonly Field Name = FindByName("Name");

            /// <summary>正常</summary>
            public static readonly Field Success = FindByName("Success");

            /// <summary>开始时间。Unix毫秒</summary>
            public static readonly Field StartTime = FindByName("StartTime");

            /// <summary>结束时间。Unix毫秒</summary>
            public static readonly Field EndTime = FindByName("EndTime");

            /// <summary>耗时。毫秒</summary>
            public static readonly Field Cost = FindByName("Cost");

            /// <summary>实例。应用可能多实例部署，ip@proccessid</summary>
            public static readonly Field ClientId = FindByName("ClientId");

            /// <summary>追踪。可用于关联多个片段，建立依赖关系，随线程上下文、Http、Rpc传递</summary>
            public static readonly Field TraceId = FindByName("TraceId");

            /// <summary>唯一标识。随线程上下文、Http、Rpc传递，作为内部片段的父级</summary>
            public static readonly Field SpanId = FindByName("SpanId");

            /// <summary>父级标识</summary>
            public static readonly Field ParentId = FindByName("ParentId");

            /// <summary>数据标签。记录一些附加数据</summary>
            public static readonly Field Tag = FindByName("Tag");

            /// <summary>错误信息</summary>
            public static readonly Field Error = FindByName("Error");

            /// <summary>创建者</summary>
            public static readonly Field CreateUser = FindByName("CreateUser");

            /// <summary>创建者</summary>
            public static readonly Field CreateUserID = FindByName("CreateUserID");

            /// <summary>创建地址</summary>
            public static readonly Field CreateIP = FindByName("CreateIP");

            /// <summary>创建时间</summary>
            public static readonly Field CreateTime = FindByName("CreateTime");

            static Field FindByName(String name) => Meta.Table.FindByName(name);
        }

        /// <summary>取得采样数据2字段名称的快捷方式</summary>
        public partial class __
        {
            /// <summary>编号</summary>
            public const String Id = "Id";

            /// <summary>数据</summary>
            public const String DataId = "DataId";

            /// <summary>应用</summary>
            public const String AppId = "AppId";

            /// <summary>跟踪项</summary>
            public const String ItemId = "ItemId";

            /// <summary>操作名。接口名或埋点名</summary>
            public const String Name = "Name";

            /// <summary>正常</summary>
            public const String Success = "Success";

            /// <summary>开始时间。Unix毫秒</summary>
            public const String StartTime = "StartTime";

            /// <summary>结束时间。Unix毫秒</summary>
            public const String EndTime = "EndTime";

            /// <summary>耗时。毫秒</summary>
            public const String Cost = "Cost";

            /// <summary>实例。应用可能多实例部署，ip@proccessid</summary>
            public const String ClientId = "ClientId";

            /// <summary>追踪。可用于关联多个片段，建立依赖关系，随线程上下文、Http、Rpc传递</summary>
            public const String TraceId = "TraceId";

            /// <summary>唯一标识。随线程上下文、Http、Rpc传递，作为内部片段的父级</summary>
            public const String SpanId = "SpanId";

            /// <summary>父级标识</summary>
            public const String ParentId = "ParentId";

            /// <summary>数据标签。记录一些附加数据</summary>
            public const String Tag = "Tag";

            /// <summary>错误信息</summary>
            public const String Error = "Error";

            /// <summary>创建者</summary>
            public const String CreateUser = "CreateUser";

            /// <summary>创建者</summary>
            public const String CreateUserID = "CreateUserID";

            /// <summary>创建地址</summary>
            public const String CreateIP = "CreateIP";

            /// <summary>创建时间</summary>
            public const String CreateTime = "CreateTime";
        }
        #endregion
    }
}